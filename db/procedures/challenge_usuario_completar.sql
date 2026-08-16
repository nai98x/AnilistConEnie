-- Suma un completado al usuario para un challenge (resolviéndolo por su nombre), acumulando la XP.
-- Devuelve la cantidad de completados resultante, o NULL si el challenge no existe o el usuario ya
-- llegó al máximo de completados permitido.
CREATE OR REPLACE FUNCTION challenge_usuario_completar(
    p_nombre  text,
    p_user_id bigint,
    p_xp      integer,
    p_fecha   timestamptz
) RETURNS integer
LANGUAGE sql
AS $$
    INSERT INTO challenge_usuarios (challenge_id, user_id, xp, fecha, completados)
    SELECT c.id, p_user_id, p_xp, p_fecha, 1
    FROM challenges c
    WHERE c.nombre = p_nombre
    ON CONFLICT (challenge_id, user_id) DO UPDATE
        SET xp = challenge_usuarios.xp + EXCLUDED.xp,
            fecha = EXCLUDED.fecha,
            completados = challenge_usuarios.completados + 1
        WHERE challenge_usuarios.completados < (
            SELECT c.max_completados FROM challenges c WHERE c.id = challenge_usuarios.challenge_id
        )
    RETURNING completados;
$$;
