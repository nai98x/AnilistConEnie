-- Challenges completados por un usuario, con los datos del challenge para mostrarlos.
CREATE OR REPLACE FUNCTION challenge_usuario_lista(p_user_id bigint)
RETURNS TABLE(user_id bigint, xp integer, nombre text, link text, disponible boolean, vencimiento timestamptz)
LANGUAGE sql
STABLE
AS $$
    SELECT cu.user_id, cu.xp, c.nombre, c.link, c.disponible, c.vencimiento
    FROM challenge_usuarios cu
    JOIN challenges c ON c.id = cu.challenge_id
    WHERE cu.user_id = p_user_id;
$$;
