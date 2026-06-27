-- Crea o actualiza el completado de un usuario para un challenge.
CREATE OR REPLACE FUNCTION challenge_usuario_upsert(
    p_challenge_id bigint,
    p_user_id      bigint,
    p_xp           integer,
    p_fecha        timestamptz
) RETURNS void
LANGUAGE sql
AS $$
    INSERT INTO challenge_usuarios (challenge_id, user_id, xp, fecha)
    VALUES (p_challenge_id, p_user_id, p_xp, p_fecha)
    ON CONFLICT (challenge_id, user_id) DO UPDATE
        SET xp = EXCLUDED.xp,
            fecha = EXCLUDED.fecha;
$$;
