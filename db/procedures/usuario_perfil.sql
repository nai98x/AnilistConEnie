-- Perfil de AniList vigente de un usuario (0 o 1 fila). Solo si está activo y tiene vínculo.
CREATE OR REPLACE FUNCTION usuario_perfil(p_user_id bigint)
RETURNS TABLE(user_id bigint, anilist_url text, message_id bigint)
LANGUAGE sql
STABLE
AS $$
    SELECT user_id, anilist_url, mensaje_id
    FROM usuarios
    WHERE user_id = p_user_id AND activo AND anilist_url IS NOT NULL;
$$;
