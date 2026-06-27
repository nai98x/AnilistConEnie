-- Baja de un usuario que dejó el server: limpia el vínculo de AniList y lo marca inactivo, pero
-- conserva la fila (y su historial relacionado) para no perder trazabilidad.
CREATE OR REPLACE FUNCTION usuario_desvincular(p_user_id bigint)
RETURNS void
LANGUAGE sql
AS $$
    UPDATE usuarios
    SET anilist_url = NULL,
        mensaje_id  = NULL,
        activo      = false
    WHERE user_id = p_user_id;
$$;
