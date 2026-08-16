-- Challenges del servidor (maestro). id sintético; el nombre es la identidad de negocio (único).
-- max_completados: cuántas veces puede completar el challenge un mismo usuario (1 = una sola vez).
CREATE TABLE IF NOT EXISTS challenges (
    id              bigint      GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nombre          text        NOT NULL UNIQUE,
    link            text        NOT NULL,
    disponible      boolean     NOT NULL,
    vencimiento     timestamptz NULL,
    max_completados integer     NOT NULL DEFAULT 1 CHECK (max_completados >= 1)
);
