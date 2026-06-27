-- Quita un id de AniList de la lista de baneados.
CREATE OR REPLACE FUNCTION anilist_baneado_delete(p_anilist_user_id integer)
RETURNS void
LANGUAGE sql
AS $$
    DELETE FROM anilist_baneados WHERE anilist_user_id = p_anilist_user_id;
$$;
