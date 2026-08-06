# Deploy

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
  `~/.config/anilist-backup/backup.env` (ver `deploy/README.md`).

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
   cp deploy-setup/anilistconenie.service ~/.config/systemd/user/
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
