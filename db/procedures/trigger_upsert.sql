-- Crea o actualiza un trigger (por nombre).
CREATE OR REPLACE FUNCTION trigger_upsert(
    p_nombre    text,
    p_texto     text,
    p_image_url text,
    p_tipo      integer
) RETURNS void
LANGUAGE sql
AS $$
    INSERT INTO triggers (nombre, texto, image_url, tipo)
    VALUES (p_nombre, p_texto, p_image_url, p_tipo)
    ON CONFLICT (nombre) DO UPDATE
        SET texto = EXCLUDED.texto,
            image_url = EXCLUDED.image_url,
            tipo = EXCLUDED.tipo;
$$;
