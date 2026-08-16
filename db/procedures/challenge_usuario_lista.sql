-- Challenges completados por un usuario, con los datos del challenge para mostrarlos.
CREATE OR REPLACE FUNCTION challenge_usuario_lista(p_user_id bigint)
RETURNS TABLE(user_id bigint, xp integer, completados integer, nombre text, link text, disponible boolean, vencimiento timestamptz, max_completados integer)
LANGUAGE sql
STABLE
AS $$
    SELECT cu.user_id, cu.xp, cu.completados, c.nombre, c.link, c.disponible, c.vencimiento, c.max_completados
    FROM challenge_usuarios cu
    JOIN challenges c ON c.id = cu.challenge_id
    WHERE cu.user_id = p_user_id;
$$;
