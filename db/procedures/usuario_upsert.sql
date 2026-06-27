-- Crea el usuario o, si ya existía, actualiza su vínculo y lo reactiva (borrado lógico revertido).
-- No toca cumple/last_activity/created_at: solo el vínculo de AniList y `activo`.
CREATE OR REPLACE FUNCTION usuario_upsert(
    p_user_id     bigint,
    p_anilist_url text,
    p_mensaje_id  bigint
) RETURNS void
LANGUAGE sql
AS $$
    INSERT INTO usuarios (user_id, anilist_url, mensaje_id, activo)
    VALUES (p_user_id, p_anilist_url, p_mensaje_id, true)
    ON CONFLICT (user_id) DO UPDATE
        SET anilist_url = EXCLUDED.anilist_url,
            mensaje_id  = EXCLUDED.mensaje_id,
            activo      = true;
$$;
