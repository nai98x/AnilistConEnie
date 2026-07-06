# CLAUDE.md

Guía para trabajar en este repo. El README tiene el detalle de arquitectura y el mapa del código; acá
van las **convenciones** a respetar al editar.

## Regla absoluta: nunca commitear ni pushear

**NUNCA** ejecutes `git commit`, `git push` ni ninguna operación que escriba en el historial o el
remoto. Los commits y pushes los hace **exclusivamente el dueño del repo, a mano**. Podés editar
archivos, correr builds/tests y preparar cambios, pero el versionado lo controla siempre la persona.

## Qué es

Bot de Discord (DSharpPlus, .NET 10) para el servidor **Añilist**. Persistencia en base de datos relacional con PostgreSQL.
Idioma del código, commits y comunicación: **español**.

## Arquitectura (Clean Architecture, 4 proyectos en `src/`)

Dirección de dependencias: **Model ← Application ← Infrastructure ← Bot**. Nunca agregar referencias
inversas.

- **AnilistConEnie.Model** — entidades, enums, excepciones e interfaces (`IAnilistClient`,
  `Interfaces/Repositories/*`). Son POCOs puros; el mapeo Firestore vive en Infrastructure vía
  `ConverterRegistry` (Model NO referencia `Google.Cloud.Firestore`).
- **AnilistConEnie.Application** — lógica de negocio. Sin dependencias de Discord ni infraestructura.
- **AnilistConEnie.Infrastructure** — repositorios Firestore, `FirebaseService`, cliente AniList.
- **AnilistConEnie.Bot** — entry point, comandos, handlers, scheduling, estado, configuración, DI.

## Dónde va el código nuevo

- **Regla/cálculo puro** (sin estado ni dependencias) → clase **estática** en Application
  (`Xp/`, `Moderation/`, `Confessions/`, `Challenges/`, `Membership/`, `Charts/`, `Helpers/`).
  Recibe sus datos **por parámetro**; no toca tipos de Discord ni de config.
- **Servicio con dependencias** → clase instancia + DI (ej. `AnilistServerScoreService` en Application,
  `AnilistService`/`GuildMaintenanceService` en Bot).
- **El "seam" con Discord queda en Bot**: resolver `RangoEnum → DiscordRole`, nombres de miembros,
  embeds y charts es responsabilidad del Bot; lo puro (XP↔rango, rankings, accrual, vencimientos) en
  Application.
- Preferí mover lógica de negocio fuera de los comandos/handlers hacia Application antes que
  engordarlos. No sobre-ingenierizar: predicados de una línea de orquestación Discord pueden quedar
  en Bot.

## Convenciones clave

- **Repositorios**: inyectar SIEMPRE por su interfaz de Model (`IXxxRepository`), nunca la clase
  concreta de Infrastructure. Solo las interfaces están registradas en el contenedor.
- **Base de datos relacional (Dapper)**: es **database-first** y se accede **solo vía stored
  procedures** (funciones PostgreSQL). Los repos de Infrastructure las invocan **por nombre** con
  `commandType: CommandType.StoredProcedure` y parámetros `p_*`; **cero SQL en el código** (ni
  `SELECT * FROM fn()`). Para que `CommandType.StoredProcedure` resuelva funciones (y no `CALL`),
  `DbConnectionFactory` activa `AppContext.SetSwitch("Npgsql.EnableStoredProcedureCompatMode", true)`.
  El esquema y los SPs viven versionados en `db/` (no en código). Los POCOs de Model mapean los
  resultados (snake_case→PascalCase vía `DefaultTypeMap.MatchNamesWithUnderscores`); la conexión se
  abre con `DbConnectionFactory` (`Infrastructure/Database/`, único archivo que conoce el motor).
- **DI de handlers/servicios**: por **constructor** (primary constructors). No usar service locator
  (`IServiceProvider.GetService`) salvo el caso ya establecido de `EventHandlerRegistrar`, donde la
  resolución de cada handler se difiere dentro de un lambda para romper el ciclo
  `DiscordClient → Handler → DiscordBotService → DiscordClient`.
- **Configuración** (ver `Bot/Configuration/`):
  - IDs de Discord (guild, canales, roles, emotes, timezones) → `BotConfiguration` + sección `Ids` de
    `appsettings.json`. `RequireUlong` falla si falta una clave.
  - Reglas de negocio tuneables (thresholds, cooldowns, durations, amounts) → **una sección por
    dominio** en `appsettings.json` (`AntiSpam`, `LimpiezaMiembros`, `Cooldowns`, `Xp`, `Confesiones`,
    `Logs`), bindeadas en `BehaviorSettings.cs` con defaults. La lógica de Application recibe esos
    valores por parámetro (no como `const`). No externalizar límites duros de Discord/AniList ni
    cosméticos.
- **Secrets**: el token de Discord es la clave `discordToken` (env var / User Secrets); los JSON de
  Firebase (`firebase-anilistconenie.json`, `firebase-yumiko.json`) van en `src/AnilistConEnie.Bot/`.
  Nada de esto se versiona.
- **Aleatoriedad**: usar `Random.Shared` salvo cuando se necesita determinismo por semilla (signos/
  ship en Fun, challenges), que se deja intacto.
- **Catch amplios**: la resiliencia de los loops (no frenar ante miembro/mensaje ausente) es
  intencional. Loguear con `DiscordLogService.LogException(guild, ex, contexto)` sin cambiar el flujo;
  `NotFoundException` es benigno (solo Debug local).
- **Estilo**: **no** agregar comentarios ni XML summaries explicando *cómo se resolvió* algo o
  justificando un cambio. Comentar solo lo no obvio del dominio, al nivel del código circundante.

## Dependencias / entorno

- DSharpPlus usa paquetes **nightly/preview** — habilitar versiones preliminares al restaurar.
- `GetInteractivity()` no existe en el nightly: resolver `InteractivityExtension` por DI.
- Está permitido leer internals/decompilados de DSharpPlus para entender comportamiento.

## Comandos

```bash
dotnet restore
dotnet build
dotnet run --project src/AnilistConEnie.Bot
```
