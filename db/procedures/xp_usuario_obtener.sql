-- Devuelve la XP del usuario (0 o 1 fila).
CREATE OR REPLACE FUNCTION xp_usuario_obtener(p_user_id bigint)
RETURNS SETOF xp_usuarios
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM xp_usuarios WHERE user_id = p_user_id;
$$;
