-- Marca a un usuario como completador de un challenge (resolviendo el challenge por su nombre).
CREATE OR REPLACE FUNCTION challenge_usuario_completar(
    p_nombre  text,
    p_user_id bigint,
    p_xp      integer,
    p_fecha   timestamptz
) RETURNS void
LANGUAGE sql
AS $$
    INSERT INTO challenge_usuarios (challenge_id, user_id, xp, fecha)
    SELECT c.id, p_user_id, p_xp, p_fecha
    FROM challenges c
    WHERE c.nombre = p_nombre
    ON CONFLICT (challenge_id, user_id) DO UPDATE
        SET xp = EXCLUDED.xp,
            fecha = EXCLUDED.fecha;
$$;
