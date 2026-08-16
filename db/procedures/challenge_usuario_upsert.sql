-- Crea o pisa el completado de un usuario para un challenge (XP y completados absolutos).
CREATE OR REPLACE FUNCTION challenge_usuario_upsert(
    p_challenge_id bigint,
    p_user_id      bigint,
    p_xp           integer,
    p_fecha        timestamptz,
    p_completados  integer
) RETURNS void
LANGUAGE sql
AS $$
    INSERT INTO challenge_usuarios (challenge_id, user_id, xp, fecha, completados)
    VALUES (p_challenge_id, p_user_id, p_xp, p_fecha, p_completados)
    ON CONFLICT (challenge_id, user_id) DO UPDATE
        SET xp = EXCLUDED.xp,
            fecha = EXCLUDED.fecha,
            completados = EXCLUDED.completados;
$$;
