# Deploy y setup del server

Todo lo que corre en la máquina: el bot y el backup de la base. Ambos son **user services** de
systemd bajo el mismo usuario (requiere `loginctl enable-linger`).

- `anilistconenie.service` — el bot.
- `backup-db.sh`, `backup-db.service`, `backup-db.timer`, `backup.env.example` — el backup diario.

El CI **solo despliega el bot**: cuando se toca cualquier otro archivo de esta carpeta hay que
copiarlo al server a mano.

---

# El bot

El bot se deploya por **CI/CD en GitHub Actions** (`.github/workflows/deploy.yml`). El job corre en
un runner **hosted de GitHub** (`ubuntu-latest`): `push` a `main` → restore → build → test →
`dotnet publish -r linux-arm64 --no-self-contained` → empaqueta un `publish.tar.gz` → lo copia al
server por **SCP** → lo descomprime en `~/bots/AnilistConEnie-app` y reinicia por **SSH** con
`systemctl --user`.

El proceso lo administra **systemd (servicio de usuario)**: sobrevive a la sesión SSH del deploy,
reinicia solo y loguea en journald.

El bot necesita **cuatro claves** para arrancar, ninguna versionada:

- `discordToken` — token del bot de Discord.
- `FIREBASE_CREDENTIALS_DIR` — carpeta que contiene los `firebase-anilistconenie.json` y
  `firebase-yumiko.json` (credenciales de Firestore/Storage).
- `ConnectionStrings:Database` — connection string de la base de datos.
- `Backups:RutaEstado` — ruta del archivo de marca del backup. No es un secreto, pero no va en
  `appsettings.json` porque es público. Tiene que coincidir con el `ESTADO` de
  `~/.config/anilist-backup/backup.env` (ver más abajo).

Las cuatro se resuelven por el **mismo mecanismo** (`IConfiguration`): en el servidor por variable de
entorno (las setea el unit de systemd) y en local por User Secrets. Son obligatorias; si falta
cualquiera, el bot falla al arrancar.

## Setup en local (una sola vez)

```bash
dotnet user-secrets --project src/AnilistConEnie.Bot set discordToken "TU_TOKEN"
dotnet user-secrets --project src/AnilistConEnie.Bot \
  set FIREBASE_CREDENTIALS_DIR /ruta/a/src/AnilistConEnie.Bot
dotnet user-secrets --project src/AnilistConEnie.Bot \
  set "ConnectionStrings:Database" 'Host=...;Port=...;Database=...;Username=...;Password=...'
dotnet user-secrets --project src/AnilistConEnie.Bot \
  set "Backups:RutaEstado" /ruta/cualquiera/ultimo-ok
```

(la carpeta de `FIREBASE_CREDENTIALS_DIR` debe contener los dos `firebase-*.json`).

> Si la password tiene `$`, usá comillas **simples** al setear el secret: en fish/bash las dobles lo
> expanden y la guardan incompleta.

## Setup en el server (una sola vez)

1. Crear directorios estables:

   ```bash
   mkdir -p ~/bots/secrets ~/bots/AnilistConEnie-app
   ```

2. Colocar en `~/bots/secrets/` los archivos que **no se versionan**:
   - `firebase-anilistconenie.json`
   - `firebase-yumiko.json`
   - `anilistconenie.env` con el token de Discord y la connection string (acá lo lee systemd, no un
     shell: los valores van literales, sin comillas):

     ```
     discordToken=...
     ConnectionStrings__Database=Host=localhost;Port=...;Database=...;Username=...;Password=...
     Backups__RutaEstado=/home/USUARIO/.config/anilist-backup/ultimo-ok
     ```

     `Backups__RutaEstado` no admite `%h` ni `$HOME`: systemd no expande nada en un
     `EnvironmentFile`, va la ruta absoluta.

   Seguridad de estos archivos y de la conexión:
   - `chmod 700 ~/bots/secrets && chmod 600 ~/bots/secrets/*` — solo el usuario del servicio debe
     poder leerlos.
   - La BD corre en el mismo server y el bot se conecta por localhost: no hace falta TLS (el
     tráfico no sale de la máquina), pero PostgreSQL debe escuchar **solo** en localhost
     (`listen_addresses = 'localhost'`, verificable con `ss -tlnp | grep 5432`) y `pg_hba.conf`
     debe exigir `scram-sha-256` para conexiones locales. Si la BD se mudara a otra máquina,
     ahí sí agregar `SSL Mode=Require` (o `VerifyFull`) a la connection string.
   - El usuario de la BD no debe ser superuser: alcanza con `CONNECT` a la base, `USAGE` del schema
     y `EXECUTE` sobre las funciones de `db/procedures/` (más los permisos de tabla que esas
     funciones necesiten si no son `SECURITY DEFINER`).

3. Instalar el servicio (`anilistconenie.service` de este directorio):

   ```bash
   cp deploy/anilistconenie.service ~/.config/systemd/user/
   loginctl enable-linger "$USER"        # arranca sin sesión iniciada
   systemctl --user daemon-reload
   systemctl --user enable anilistconenie
   ```

4. En GitHub (Settings → Secrets and variables → Actions), configurar los secrets que usa el deploy
   por SSH/SCP:
   - `HOST` — host o IP del server.
   - `USERNAME` — usuario SSH (el mismo que corre el servicio de systemd).
   - `PRIVATE_KEY` — clave privada SSH con acceso a ese usuario.

## Operación

```bash
systemctl --user status anilistconenie
journalctl --user -u anilistconenie -f
systemctl --user restart anilistconenie
```

---

# Backup de la base

Dump diario de PostgreSQL, cifrado, subido a un remote de rclone, conservando las últimas N copias.

Los valores concretos (base, remote, bucket) **no se versionan**: viven en
`~/.config/anilist-backup/backup.env` en el server. Ver `backup.env.example`.

## Piezas

- `backup-db.sh` → `~/bin/backup-db.sh` (`chmod +x`)
- `backup-db.service` / `backup-db.timer` → `~/.config/systemd/user/`
- `backup.env.example` → copiar a `~/.config/anilist-backup/backup.env` (`chmod 600`) y completar

Si el script queda desactualizado el backup puede seguir subiendo y fallar después, sin que se note
salvo por el aviso del bot.

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
`~/bots/secrets/`, que no están versionados.

## Aviso de fallos

Después de subir, el script escribe la fecha en el archivo de `ESTADO`. El bot
(`BackupScheduledService`) lo lee todos los días a las 09:00 y, si la marca no es la de hoy, avisa en
el canal de config. La ruta le llega al bot por la variable de entorno `Backups__RutaEstado` (no va en
`appsettings.json`, que es público) y tiene que coincidir con la del `backup.env`.

Verifica que el script terminó bien, no que el archivo siga existiendo en el proveedor: cubre los
fallos reales (timer caído, credenciales vencidas, dump roto) pero no que alguien borre el bucket.

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
