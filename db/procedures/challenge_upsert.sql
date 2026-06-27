-- Crea o actualiza un challenge (por nombre) y devuelve su id.
CREATE OR REPLACE FUNCTION challenge_upsert(
    p_nombre      text,
    p_link        text,
    p_disponible  boolean,
    p_vencimiento timestamptz
) RETURNS bigint
LANGUAGE sql
AS $$
    INSERT INTO challenges (nombre, link, disponible, vencimiento)
    VALUES (p_nombre, p_link, p_disponible, p_vencimiento)
    ON CONFLICT (nombre) DO UPDATE
        SET link = EXCLUDED.link,
            disponible = EXCLUDED.disponible,
            vencimiento = EXCLUDED.vencimiento
    RETURNING id;
$$;
