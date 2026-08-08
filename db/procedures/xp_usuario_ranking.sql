-- Devuelve la XP de todos los usuarios (rankings del servidor).
CREATE OR REPLACE FUNCTION xp_usuario_ranking()
RETURNS SETOF xp_usuarios
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM xp_usuarios;
$$;
