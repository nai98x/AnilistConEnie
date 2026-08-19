# Base de datos

Esquema y stored procedures versionados. La base es **database-first**: el esquema se diseña acá (es la
fuente de verdad) y el código C# se adapta. El acceso desde el bot es vía **Dapper invocando stored
procedures** — nunca SQL embebido en el código.

## Estructura

```
db/
  schema/        # tablas, tipos, índices, constraints
  procedures/    # un .sql por stored procedure
```

## Convenciones de los scripts

- **Idempotentes**: cada script se puede correr más de una vez sin romper (`CREATE TABLE IF NOT
  EXISTS`, `CREATE OR REPLACE FUNCTION`, etc.).
- **Un stored procedure por archivo** en `procedures/`, con el nombre del SP como nombre de archivo.
- Cambios de esquema y los SPs que los consumen van en el **mismo commit** que el código C# que los usa.
- **Las migraciones no se versionan**: `schema/` refleja siempre el estado actual de cada tabla (el
  `CREATE` limpio), sin `ALTER`, sin bloques `DO $$` ni scripts de datos. El SQL de migración se corre
  a mano y solo se versiona el estado final.

## Aplicar los scripts

Conectado a la base (por DBeaver o `psql`), correr primero los de `schema/` y después los de
`procedures/`. Como son idempotentes, reaplicarlos sincroniza la base con lo versionado.

## La base de Yumiko

`db/` versiona **solo la base propia**. El bot además escribe el vínculo de AniList en la base de
**Yumiko** (otra base, con su propio rol) llamando a `anilist_user_upsert(bigint, integer)`: ese SP y
la tabla `anilist_users` viven en el repo de Yumiko, no acá. El rol del bot tiene ahí solo `EXECUTE`
sobre esa función y `SELECT`/`INSERT`/`UPDATE` sobre esa tabla: el `ON CONFLICT DO UPDATE` del SP lee
`EXCLUDED`, así que el `SELECT` no es opcional.

## Seguridad del rol del bot

El usuario con el que se conecta el bot no debe ser superuser ni dueño del schema: alcanza con
`CONNECT` a la base, `USAGE` del schema y `EXECUTE` sobre las funciones de `procedures/` (más los
permisos de tabla que esas funciones necesiten). La BD vive en el mismo server que el bot y debe
escuchar solo en localhost (ver `deploy/README.md`); TLS solo haría falta si se mudara a
otra máquina.
