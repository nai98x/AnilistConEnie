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

## Aplicar los scripts

Conectado a la base (por DBeaver o `psql`), correr primero los de `schema/` y después los de
`procedures/`. Como son idempotentes, reaplicarlos sincroniza la base con lo versionado.
