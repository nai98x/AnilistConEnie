-- Todos los ids de AniList baneados.
CREATE OR REPLACE FUNCTION anilist_baneados_lista()
RETURNS TABLE(anilist_user_id integer)
LANGUAGE sql
STABLE
AS $$
    SELECT anilist_user_id FROM anilist_baneados;
$$;
