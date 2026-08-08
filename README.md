# Añilist

[![CodeFactor](https://www.codefactor.io/repository/github/nai98x/anilistconenie/badge)](https://www.codefactor.io/repository/github/nai98x/anilistconenie)

Bot multipropósito para el servidor de Discord **Añilist**, desarrollado en DSharpPlus sobre .NET 10.

## Stack

- **.NET 10** — lenguaje de programación para toda la solución.
- **DSharpPlus** — librería de Discord (se usan paquetes *nightly/preview*).
- **Base de datos relacional (Dapper)** — *database-first* y accedida vía stored procedures.
- **Firebase** — Uso en Storage (imágenes de `/subirimagen`) y la base externa *Yumiko* (espejo de vinculación de AniList).
- **AniList GraphQL API** — datos de animes, mangas y perfiles.
- **ScottPlot / SkiaSharp** — generación de charts.
- **Serilog** — logging a consola y archivo.

## Arquitectura

La solución (`AnilistConEnie.sln`) sigue una arquitectura limpia con 4 proyectos en `src/`. La
dirección de dependencias es **Model ← Application ← Infrastructure ← Bot**: cada capa solo conoce a
las de adentro.

| Proyecto | Responsabilidad |
|----------|-----------------|
| **AnilistConEnie.Model** | Entidades, enums, excepciones e interfaces. Define los contratos (`Interfaces/IAnilistClient.cs`, `Interfaces/Repositories/*`) que implementa Infrastructure. Sin dependencias hacia las otras capas. |
| **AnilistConEnie.Application** | Lógica de negocio, mayormente como clases estáticas sin estado: `Xp/`, `Moderation/`, `Confessions/`, `Challenges/`, `Membership/`, `Charts/`, `Anilist/`, `Backups/`, `Fun/`, `Premios/`, `Triggers/`, `Helpers/`. No depende de Discord ni de la infraestructura. |
| **AnilistConEnie.Infrastructure** | Acceso a datos y servicios externos: `Database/DbConnectionFactory.cs` + `Repositories/*` (Dapper, stored procedures), `Firebase/FirebaseService.cs` + `Repositories/FirebaseRepository.cs` (Storage + Yumiko), `Anilist/` (cliente GraphQL: `AnilistClient`, `AnilistGraphQLExecutor`, `AnilistQueries`) y `Charts/`. |
| **AnilistConEnie.Bot** | Punto de entrada y todo lo relacionado a Discord: comandos, handlers de eventos, tareas programadas, estado en memoria, configuración y configuración de inyección de dependencias. |

**Decisiones de diseño:**

- El **estado en memoria** del bot usa cache en singletons separados por responsabilidad (`Bot/Services/State/*`).
- La **lógica de negocio** se mantiene en Application, preferentemente como funciones estáticas
  (reciben sus datos por parámetro), de modo que el Bot solo orquesta. Lo que necesita dependencias
  va como servicio de instancia (`AnilistServerScoreService`, `XpChartService`).
- El acceso a **AniList** está centralizado en un único cliente (`Infrastructure/Anilist/`) con
  reintentos (Polly) y rate limit, detrás de la interfaz `IAnilistClient`.

## Mapa del código

| Qué | Dónde |
|-----|-------|
| **Entry point / arranque** | `Bot/Program.cs` — configura Serilog, construye el `Host` y registra todo el DI. |
| **Cableado de Discord** | `Bot/Extensions/ServiceCollectionExtensions.cs` — cliente de Discord, interactivity, comandos y lectura del token. |
| **Servicios del Bot en DI** | `Bot/Extensions/BotServiceExtensions.cs` — estado, helpers y hosted services. |
| **Servicios por capa en DI** | `Application/Extensions/ApplicationServiceExtensions.cs`, `Infrastructure/Extensions/InfrastructureServiceExtensions.cs`. |
| **Configuración** | `Bot/Configuration/BotConfiguration.cs` (IDs de Discord) y `Bot/Configuration/BehaviorSettings.cs` (parámetros configurables). |
| **Slash commands** | `Bot/Commands/Slash/*.cs` — `Admin`, `Anilist`, `Challenges`, `Fun`, `Owner`, `Premios`, `Teiou`, `Triggers`, `Usuarios`, `Xp`. Se autoregistran vía `AddDiscoveredSlashCommands` (`Bot/Extensions/CommandsExtensionExtensions.cs`). |
| **Otros comandos** | `Bot/Commands/Text/` y `Bot/Commands/ContextMenu/`, con sus propios `AddDiscovered*`. |
| **Infra de comandos** | `Bot/Commands/Framework/` — `Checks/`, `Attributes/`, `Choices/`, `AutoComplete/` y `CommandErrorHandler.cs`. |
| **Handlers de eventos** | `Bot/Events/Handlers/*` (`Message*`, `GuildMember*`, `ComponentInteraction`, `MessageReactionAdded`, `Session*`, `GuildDownloadCompleted`, `Zombied`). Se registran y configuran en `Bot/Events/EventHandlerRegistrar.cs` (resolución diferida vía `ServiceProvider` para evitar el ciclo de dependencias en el arranque). |
| **Tareas programadas** | `Bot/Services/Scheduling/CronBackgroundService.cs` y `Tasks/*` (`Minute`, `Hourly`, `Daily`, `Backup`), como hosted services. |
| **Estado en memoria** | `Bot/Services/State/*` (`XpState`, `TriggersState`, `ConfessionsState`, `HackedAccountState`, etc.). |
| **Helpers / servicios del Bot** | `Bot/Helpers/*` (`AnilistService`, `GuildMaintenanceService`, `DiscordLogService`, `RangoRoles`, `DiscordInteractivity`, …). |
| **Servicio principal** | `Bot/Services/DiscordBotService.cs` — hosted service que conecta y mantiene el cliente. |

## Configuración

### `appsettings.json`

En `src/AnilistConEnie.Bot/appsettings.json`. Tiene dos bloques:

- **`Ids`** — IDs del servidor, canales, roles, emotes y timezones. Se bindean en `BotConfiguration.cs`.
- **Reglas de negocio configurables** — secciones `AntiSpam`, `LimpiezaMiembros`, `Cooldowns`, `Xp`,
  `Confesiones`, `SubirImagen` y `Logs`. Se bindean en `BehaviorSettings.cs` (cada sección tiene
  sus defaults).

### Token de Discord

Se provee por fuera mediante la clave **`discordToken`**: variable de entorno o User
Secrets con la misma clave. Si falta o es inválido, el bot falla al iniciar.

### Credenciales de Firebase

Dos archivos JSON de cuenta de servicio con estos nombres exactos (los que carga
`Infrastructure/Firebase/FirebaseService.cs`):

- `firebase-anilistconenie.json` — solo para **Storage** (subida de imágenes de `/subirimagen`).
- `firebase-yumiko.json` — base externa **Yumiko**, donde se espeja el vínculo de AniList.

La carpeta que los contiene se indica por fuera con la clave **`FIREBASE_CREDENTIALS_DIR`**.

### Base de datos

Persistencia principal. Acceso vía **Dapper**, centralizado en
`Infrastructure/Database/DbConnectionFactory.cs`. Es
*database-first* y se accede **solo vía stored procedures**; el esquema y los procedimientos están
dentro de la carpeta `db/`. La connection string se provee por fuera con la clave
**`ConnectionStrings:Database`**: variable de entorno o User Secrets. Detalle
de setup en `deploy/README.md`.

## Requisitos y ejecución

- .NET 10 SDK.
- Restaurar paquetes (algunos de DSharpPlus son *preview*, hay que permitir versiones preliminares):

```bash
dotnet restore
```

- Ejecutar el bot:

```bash
dotnet run --project src/AnilistConEnie.Bot
```
