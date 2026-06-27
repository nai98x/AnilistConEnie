-- Devuelve la XP de todos los usuarios (para el cache de ranking del servidor).
CREATE OR REPLACE FUNCTION xp_usuario_ranking()
RETURNS SETOF xp_usuarios
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM xp_usuarios;
$$;
