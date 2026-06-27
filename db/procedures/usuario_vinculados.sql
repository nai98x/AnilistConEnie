-- Usuarios activos con vínculo de AniList vigente (para el estado en memoria y la lista de perfiles).
CREATE OR REPLACE FUNCTION usuario_vinculados()
RETURNS TABLE(user_id bigint, anilist_url text, message_id bigint)
LANGUAGE sql
STABLE
AS $$
    SELECT user_id, anilist_url, mensaje_id
    FROM usuarios
    WHERE activo AND anilist_url IS NOT NULL;
$$;
