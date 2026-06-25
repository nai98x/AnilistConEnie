# Deploy

El bot se deploya por **CI/CD en GitHub Actions** (`.github/workflows/deploy.yml`). El job corre en
un runner **hosted de GitHub** (`ubuntu-latest`): `push` a `main` → restore → build → test →
`dotnet publish -r linux-arm64 --no-self-contained` → empaqueta un `publish.tar.gz` → lo copia al
server por **SCP** → lo descomprime en `~/bots/AnilistConEnie-app` y reinicia por **SSH** con
`systemctl --user`.

El proceso lo administra **systemd (servicio de usuario)**: sobrevive a la sesión SSH del deploy,
reinicia solo y loguea en journald.

El bot necesita **dos secretos** para arrancar, ninguno versionado:

- `discordToken` — token del bot de Discord.
- `FIREBASE_CREDENTIALS_DIR` — carpeta que contiene los `firebase-anilistconenie.json` y
  `firebase-yumiko.json` (credenciales de Firestore/Storage).

Ambos se resuelven por el **mismo mecanismo** (`IConfiguration`): en el servidor por variable de
entorno (las setea el unit de systemd) y en local por User Secrets. Son obligatorios; si falta
cualquiera, el bot falla al arrancar.

## Setup en local (una sola vez)

```bash
dotnet user-secrets --project src/AnilistConEnie.Bot set discordToken "TU_TOKEN"
dotnet user-secrets --project src/AnilistConEnie.Bot \
  set FIREBASE_CREDENTIALS_DIR /ruta/a/src/AnilistConEnie.Bot
```

(la carpeta de `FIREBASE_CREDENTIALS_DIR` debe contener los dos `firebase-*.json`).

## Setup en el server (una sola vez)

1. Crear directorios estables:

   ```bash
   mkdir -p ~/bots/secrets ~/bots/AnilistConEnie-app
   ```

2. Colocar en `~/bots/secrets/` los archivos que **no se versionan**:
   - `firebase-anilistconenie.json`
   - `firebase-yumiko.json`
   - `anilistconenie.env` con el token de Discord:

     ```
     discordToken=...
     ```

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
