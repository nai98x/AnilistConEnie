-- Triggers de respuesta automática. El nombre es la identidad (catálogo standalone, sin FK).
CREATE TABLE IF NOT EXISTS triggers (
    nombre    text    PRIMARY KEY,
    texto     text    NOT NULL,
    image_url text    NOT NULL,
    tipo      integer NOT NULL
);
