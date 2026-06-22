# Añilist
## Bot para Discord desarrollado en DSharpPlus - .NET 10
[![CodeFactor](https://www.codefactor.io/repository/github/nai98x/anilistconenie/badge?s=57db35bd2ed0cf54a69895b0830d6d4b45966edd)](https://www.codefactor.io/repository/github/nai98x/anilistconenie)

Bot multi propósito para el servidor de Discord Anilist ESP.

## Arquitectura

La solución (`AnilistConEnie.sln`) sigue una arquitectura limpia con 4 proyectos en `src/`:

- **AnilistConEnie.Model** — entidades, enums e interfaces.
- **AnilistConEnie.Application** — lógica de aplicación y helpers.
- **AnilistConEnie.Infrastructure** — acceso a datos (Firebase/Firestore) y servicios externos.
- **AnilistConEnie.Bot** — punto de entrada, cliente de Discord (DSharpPlus), handlers de eventos y configuración.

## Requisitos previos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Un IDE: [JetBrains Rider](https://www.jetbrains.com/rider/) o [Visual Studio](https://visualstudio.microsoft.com/) (con la carga de trabajo *Desarrollo multiplataforma de .NET*)
- Una cuenta de [Firebase](https://firebase.google.com/) (plan Spark gratuito alcanza)
- Una aplicación de bot en el [Discord Developer Portal](https://discord.com/developers/applications)

## Instalación

### 1. Clonar el repositorio y abrir la solución

Cloná el repo y abrí `AnilistConEnie.sln` en tu IDE.

### 2. Restaurar paquetes NuGet

Algunos paquetes (DSharpPlus) son versiones *nightly/preview*. Desde la raíz del repositorio:

```bash
dotnet restore
```

> En Visual Studio, activá **Incluir versión preliminar** en el administrador de NuGet si los restaurás desde ahí.

### 3. Configurar Firebase

El bot usa dos bases de datos de Firestore (cuentas de servicio separadas):

- Ir al [sitio web](https://firebase.google.com/) de Firebase
- Crear el/los proyecto(s) si no los tenés (el plan Spark gratuito alcanza)
- Ir a **Firestore** y crear la base de datos en modo test
- Quitar la regla de expiración del testeo
- Ir a **Project settings → Service accounts** y generar una clave privada (JSON)

Colocá los dos archivos de credenciales en `src/AnilistConEnie.Bot/` con estos nombres exactos (son los que carga `FirebaseService`):

- `firebase-anilistconenie.json`
- `firebase-yumiko.json`

Estos archivos **no se versionan** (están excluidos del repositorio) y se copian automáticamente al directorio de salida al compilar.

### 4. Configurar los IDs del bot

Los IDs de servidor, canales, roles y emotes se configuran en [`src/AnilistConEnie.Bot/appsettings.json`](src/AnilistConEnie.Bot/appsettings.json) (sección `Ids`). Este archivo **sí** se versiona (no contiene secrets).

### 5. Token de Discord

El token **no se versiona** (no está en `appsettings.json` ni en el repositorio). Se provee por fuera, según el entorno. El bot lo lee de la clave `discordToken`, con esta prioridad: `appsettings.json` → User Secrets → variables de entorno (la variable de entorno gana).

#### Local (máquina de desarrollo) → User Secrets

El proyecto ya tiene `UserSecretsId` configurado en `AnilistConEnie.Bot.csproj`, así que solo hay que setear el valor (se guarda fuera del repo, en `~/.microsoft/usersecrets/`):

```bash
dotnet user-secrets set "discordToken" "TU_TOKEN_AQUI" --project src/AnilistConEnie.Bot
```

Para verificar que quedó guardado:

```bash
dotnet user-secrets list --project src/AnilistConEnie.Bot
# Debe mostrar: discordToken = ...
```

#### Servidor (deploy) → Variable de entorno

Setear la variable de entorno `discordToken` al ejecutar el bot:

```bash
discordToken="TU_TOKEN_AQUI" dotnet AnilistConEnie.Bot.dll
```

O, si corre como servicio `systemd`, en el archivo `.service`:

```ini
[Service]
Environment=discordToken=TU_TOKEN_AQUI
```

Para verificar que la variable está disponible en la sesión:

```bash
echo $discordToken
```

> El token se obtiene en el [Discord Developer Portal](https://discord.com/developers/applications) → tu aplicación → **Bot** → *Reset Token*. Nunca lo subas al repositorio ni lo compartas.

#### Chequeo general

Si el token falta o es inválido, el bot lanza al iniciar:

```
'discordToken' es obligatorio: configuralo via User Secrets (local) o variable de entorno 'discordToken' (servidor)
```

### 6. Ejecutar el bot

Desde tu IDE (proyecto de inicio `AnilistConEnie.Bot`) o por consola:

```bash
dotnet run --project src/AnilistConEnie.Bot
```

### Listo!
