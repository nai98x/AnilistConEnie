-- ¿El id de AniList está baneado? (para el chequeo en el intento de vínculo).
CREATE OR REPLACE FUNCTION anilist_baneado_existe(p_anilist_user_id integer)
RETURNS boolean
LANGUAGE sql
STABLE
AS $$
    SELECT EXISTS(SELECT 1 FROM anilist_baneados WHERE anilist_user_id = p_anilist_user_id);
$$;
