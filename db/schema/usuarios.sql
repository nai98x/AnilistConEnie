-- Usuarios del servidor. PK = ID de Discord.
-- Borrado lógico vía `activo`: nunca se borra físicamente, ni en cascada desde tablas relacionadas
-- (XP, etc.). Aunque el usuario deje el server, la fila se conserva para no perder trazabilidad.
CREATE TABLE IF NOT EXISTS usuarios (
    user_id       bigint      PRIMARY KEY,                              -- ID de Discord
    anilist_url   text        NULL,                                     -- null en ex-miembros sin vínculo vigente (migrados por su historial)
    mensaje_id    bigint      NULL,                                     -- mensaje de vinculación (no FK)
    cumple_dia    smallint    NULL CHECK (cumple_dia BETWEEN 1 AND 31),
    cumple_mes    smallint    NULL CHECK (cumple_mes BETWEEN 1 AND 12),
    activo        boolean     NOT NULL DEFAULT true,
    last_activity timestamptz NULL,
    fecha_entrada timestamptz NULL,                                     -- fecha real de entrada al server cuando difiere del joined_at de Discord (fundadores/aniversarios)
    CONSTRAINT cumple_dia_mes_juntos CHECK ((cumple_dia IS NULL) = (cumple_mes IS NULL))
);
