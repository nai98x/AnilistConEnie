-- Crea o actualiza un premio (por nombre).
CREATE OR REPLACE FUNCTION premio_upsert(
    p_nombre text,
    p_link   text,
    p_anio   integer,
    p_orden  integer
) RETURNS void
LANGUAGE sql
AS $$
    INSERT INTO premios (nombre, link, anio, orden)
    VALUES (p_nombre, p_link, p_anio, p_orden)
    ON CONFLICT (nombre) DO UPDATE
        SET link = EXCLUDED.link,
            anio = EXCLUDED.anio,
            orden = EXCLUDED.orden;
$$;
