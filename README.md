# Añilist

[![CodeFactor](https://www.codefactor.io/repository/github/nai98x/anilistconenie/badge?s=57db35bd2ed0cf54a69895b0830d6d4b45966edd)](https://www.codefactor.io/repository/github/nai98x/anilistconenie)

Bot multipropósito para el servidor de Discord **Anilist ESP**, desarrollado en DSharpPlus sobre .NET 10.

## Stack

- **.NET 10** — la solución completa.
- **DSharpPlus** — librería de Discord (se usan paquetes *nightly/preview*).
- **Firestore (Firebase)** — persistencia (dos bases de datos, cuentas de servicio separadas).
- **AniList GraphQL API** — datos de animes, mangas y perfiles.
- **QuickChart** — generación de gráficos (XP, charts varios).
- **Serilog** — logging a consola y archivo.

## Arquitectura

La solución (`AnilistConEnie.sln`) sigue una arquitectura limpia con 4 proyectos en `src/`. La
dirección de dependencias es **Model ← Application ← Infrastructure ← Bot**: cada capa solo conoce a
las de adentro.

| Proyecto | Responsabilidad |
|----------|-----------------|
| **AnilistConEnie.Model** | Entidades, enums, excepciones e interfaces. Define los contratos (`Interfaces/IAnilistClient.cs`, `Interfaces/Repositories/*`) que implementa Infrastructure. Sin dependencias hacia las otras capas. |
| **AnilistConEnie.Application** | Lógica de negocio **pura**, como clases estáticas sin estado: `Xp/`, `Moderation/`, `Confessions/`, `Challenges/`, `Membership/`, `Charts/`, `Helpers/`. No depende de Discord ni de la infraestructura. |
| **AnilistConEnie.Infrastructure** | Acceso a datos y servicios externos: `Firebase/FirebaseService.cs`, `Repositories/*` (Firestore), `Anilist/` (cliente GraphQL: `AnilistClient`, `AnilistGraphQLExecutor`, `AnilistQueries`) y `Charts/`. |
| **AnilistConEnie.Bot** | Punto de entrada y todo lo relacionado a Discord: comandos, handlers de eventos, tareas programadas, estado en memoria, configuración y cableado de DI. |

**Decisiones clave:**

- El **estado en memoria** del bot vive en singletons separados por responsabilidad (`Bot/Services/State/*`).
- La **lógica de negocio** se mantiene en Application como funciones estáticas puras (reciben sus
  datos por parámetro), de modo que el Bot solo orquesta.
- El acceso a **AniList** está centralizado en un único cliente (`Infrastructure/Anilist/`) con
  reintentos (Polly) y rate limit, detrás de la interfaz `IAnilistClient`.

## Mapa del código (dónde está cada cosa)

| Qué | Dónde |
|-----|-------|
| **Entry point / arranque** | `Bot/Program.cs` — configura Serilog, construye el `Host` y registra todo el DI. |
| **Cableado de Discord** | `Bot/Extensions/ServiceCollectionExtensions.cs` — cliente de Discord, interactivity, comandos y lectura del token. |
| **Servicios del Bot en DI** | `Bot/Extensions/BotServiceExtensions.cs` — estado, helpers y hosted services. |
| **Servicios por capa en DI** | `Application/Extensions/ApplicationServiceExtensions.cs`, `Infrastructure/Extensions/InfrastructureServiceExtensions.cs`. |
| **Configuración** | `Bot/Configuration/BotConfiguration.cs` (IDs de Discord) y `Bot/Configuration/BehaviorSettings.cs` (reglas de negocio tuneables). |
| **Slash commands** | `Bot/Commands/SlashCommands/*.cs` — `Admin`, `Anilist`, `Challenges`, `Fun`, `Owner`, `Premios`, `Teiou`, `Triggers`, `Usuarios`, `Xp`. Se autoregistran vía `AddDiscoveredSlashCommands`. |
| **Autocomplete** | `Bot/Commands/AutoComplete/`. |
| **Handlers de eventos** | `Bot/Events/Handlers/*` (`Message*`, `GuildMember*`, `ComponentInteraction`, `MessageReactionAdded`, `Session*`, `GuildDownloadCompleted`, `Zombied`). Se registran y cablean en `Bot/Events/EventHandlerRegistrar.cs` (resolución diferida vía `ServiceProvider` para evitar el ciclo de dependencias en el arranque). |
| **Tareas programadas** | `Bot/Services/Scheduling/CronBackgroundService.cs` y `Tasks/*` (`Minute`, `Hourly`, `Daily`, `Annual`), como hosted services. |
| **Estado en memoria** | `Bot/Services/State/*` (`XpState`, `TriggersState`, `ConfessionsState`, `HackedAccountState`, etc.). |
| **Helpers / servicios del Bot** | `Bot/Helpers/*` (`AnilistService`, `GuildMaintenanceService`, `DiscordLogService`, `RangoRoles`, `DiscordInteractivity`, …). |
| **Servicio principal** | `Bot/Services/DiscordBotService.cs` — hosted service que conecta y mantiene el cliente. |

## Configuración

### `appsettings.json` (se versiona)

En `src/AnilistConEnie.Bot/appsettings.json`. No contiene secrets. Tiene dos bloques:

- **`Ids`** — IDs del servidor, canales, roles, emotes y timezones. Se bindean en `BotConfiguration.cs`.
- **Reglas de negocio tuneables** — secciones `AntiSpam`, `LimpiezaMiembros`, `Cooldowns`, `Xp`,
  `Confesiones` y `Logs`. Se bindean en `BehaviorSettings.cs` (cada sección tiene defaults razonables).

### Token de Discord (no se versiona)

Se provee por fuera mediante la clave **`discordToken`**: variable de entorno en el servidor, o User
Secrets con la misma clave en desarrollo local. Si falta o es inválido, el bot falla al iniciar.

### Credenciales de Firebase (no se versionan)

Dos archivos JSON de cuenta de servicio, ubicados en `src/AnilistConEnie.Bot/` con estos nombres
exactos (los que carga `Infrastructure/Firebase/FirebaseService.cs`):

- `firebase-anilistconenie.json`
- `firebase-yumiko.json`

## Requisitos y ejecución

- [.NET 10 SDK](https://dotnet.microsoft.com/download).
- Restaurar paquetes (algunos de DSharpPlus son *preview*, hay que permitir versiones preliminares):

```bash
dotnet restore
```

- Ejecutar el bot:

```bash
dotnet run --project src/AnilistConEnie.Bot
```

## Licencia

Ver [`LICENSE.md`](LICENSE.md).
