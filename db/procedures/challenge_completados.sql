-- Usuarios que completaron un challenge (por nombre), con la XP y fecha del completado.
CREATE OR REPLACE FUNCTION challenge_completados(p_nombre text)
RETURNS TABLE(user_id bigint, xp integer, "date" timestamptz)
LANGUAGE sql
STABLE
AS $$
    SELECT cu.user_id, cu.xp, cu.fecha
    FROM challenge_usuarios cu
    JOIN challenges c ON c.id = cu.challenge_id
    WHERE c.nombre = p_nombre;
$$;
