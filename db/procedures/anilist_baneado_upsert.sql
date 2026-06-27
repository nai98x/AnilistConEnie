-- Agrega un id de AniList baneado (no pisa si ya existe).
CREATE OR REPLACE FUNCTION anilist_baneado_upsert(p_anilist_user_id integer)
RETURNS void
LANGUAGE sql
AS $$
    INSERT INTO anilist_baneados (anilist_user_id)
    VALUES (p_anilist_user_id)
    ON CONFLICT (anilist_user_id) DO NOTHING;
$$;
