-- Crea el usuario o, si ya existía, actualiza su vínculo y lo reactiva (borrado lógico revertido).
-- No toca cumple/last_activity/created_at: solo el vínculo de AniList y `activo`.
-- `fecha_entrada` se registra una única vez: si ya había una (primera entrada), se conserva.
CREATE OR REPLACE FUNCTION usuario_upsert(
    p_user_id       bigint,
    p_anilist_url   text,
    p_mensaje_id    bigint,
    p_fecha_entrada timestamptz
) RETURNS void
LANGUAGE sql
AS $$
    INSERT INTO usuarios (user_id, anilist_url, mensaje_id, activo, fecha_entrada)
    VALUES (p_user_id, p_anilist_url, p_mensaje_id, true, p_fecha_entrada)
    ON CONFLICT (user_id) DO UPDATE
        SET anilist_url   = EXCLUDED.anilist_url,
            mensaje_id    = EXCLUDED.mensaje_id,
            activo        = true,
            fecha_entrada = COALESCE(usuarios.fecha_entrada, EXCLUDED.fecha_entrada);
$$;
