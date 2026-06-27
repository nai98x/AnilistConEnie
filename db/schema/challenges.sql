-- Challenges del servidor (maestro). id sintético; el nombre es la identidad de negocio (único).
CREATE TABLE IF NOT EXISTS challenges (
    id          bigint      GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nombre      text        NOT NULL UNIQUE,
    link        text        NOT NULL,
    disponible  boolean     NOT NULL,
    vencimiento timestamptz NULL
);
