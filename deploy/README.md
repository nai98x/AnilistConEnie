# Backup de la base

Dump diario de PostgreSQL, cifrado, subido a un remote de rclone, conservando las últimas N copias.
Corre como **user service** de systemd con el mismo usuario que el bot (requiere `Linger=yes`).

Los valores concretos (base, remote, bucket) **no se versionan**: viven en
`~/.config/anilist-backup/backup.env` en el server. Ver `backup.env.example`.

## Piezas

- `backup-db.sh` → `~/bin/backup-db.sh` (`chmod +x`)
- `backup-db.service` / `backup-db.timer` → `~/.config/systemd/user/`
- `backup.env.example` → copiar a `~/.config/anilist-backup/backup.env` (`chmod 600`) y completar

## Requisitos en el server

1. **rclone** en `~/bin/rclone` con un remote configurado (`rclone config`).
   Si la key del proveedor está restringida a un bucket, `rclone lsd remote:` falla por diseño:
   verificar con `rclone lsf remote:bucket`.

2. **Rol de solo lectura** para el dump (el rol del bot solo tiene `EXECUTE` sobre las funciones):

   ```sql
   CREATE ROLE backup_ro LOGIN PASSWORD '...';
   GRANT CONNECT ON DATABASE <base> TO backup_ro;
   GRANT USAGE ON SCHEMA public TO backup_ro;
   GRANT pg_read_all_data TO backup_ro;  -- PostgreSQL 14+
   ```

   Y `~/.pgpass` (`chmod 600`) con `localhost:5432:<base>:backup_ro:<password>`.

3. **Passphrase** de cifrado en `~/.config/anilist-backup/passphrase` (`chmod 600`), generada con
   `head -c 32 /dev/urandom | base64`. **Guardarla fuera del server** (gestor de contraseñas): es el
   único dato del setup que no se puede recuperar si se pierde la máquina, y sin ella los backups no
   sirven para nada.

4. Si el proveedor versiona objetos (ej. Backblaze B2), configurar el lifecycle del bucket en
   *keep only the last version*. Si no, las versiones ocultas de los borrados se acumulan y facturan.

## Instalación

```bash
chmod +x ~/bin/backup-db.sh
systemctl --user daemon-reload
systemctl --user enable --now backup-db.timer
systemctl --user list-timers backup-db.timer
```

`OnCalendar` se interpreta en la **hora local del server**, no en UTC.

## Restore

```bash
rclone copy <remote>/<archivo>.dump.gpg /tmp/
gpg --batch --pinentry-mode loopback --passphrase-file ~/.config/anilist-backup/passphrase \
    -o /tmp/v.dump -d /tmp/<archivo>.dump.gpg
pg_restore -d <base_destino> --no-owner --clean --if-exists /tmp/v.dump
```

`--no-owner` porque el dump lo genera `backup_ro` y el restore corre con otro rol.

El dump cubre una sola base: **no incluye los roles ni sus permisos**, que se recrean con el SQL de
arriba y lo de `db/README.md`. Tampoco cubre los JSON de credenciales de Firebase de
`src/AnilistConEnie.Bot/`, que no están versionados.

## Verificación

Conviene reprobar el restore cada tanto contra una base descartable, comparando contra la viva:

```bash
psql -c "select count(*) from usuarios"                                       # base viva
psql -d prueba_restore -c "select count(*) from usuarios"
psql -d prueba_restore -c "select count(*) from pg_proc where pronamespace = 'public'::regnamespace"
```

El último tiene que coincidir con la cantidad de archivos de `db/procedures/`.

El journal de usuario puede no estar persistido; en ese caso los logs se leen desde el system journal:

```bash
sudo journalctl _SYSTEMD_USER_UNIT=backup-db.service -n 50 --no-pager
```
