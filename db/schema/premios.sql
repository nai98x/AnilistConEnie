-- Premios por temporada. El nombre ("{temporada} {año}") es la identidad (catálogo standalone).
CREATE TABLE IF NOT EXISTS premios (
    nombre text    PRIMARY KEY,
    link   text    NOT NULL,
    anio   integer NOT NULL,
    orden  integer NOT NULL
);
