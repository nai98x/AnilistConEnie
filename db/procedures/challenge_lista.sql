-- Todos los challenges del servidor (maestro).
CREATE OR REPLACE FUNCTION challenge_lista()
RETURNS TABLE(nombre text, link text, disponible boolean, vencimiento timestamptz, max_completados integer)
LANGUAGE sql
STABLE
AS $$
    SELECT nombre, link, disponible, vencimiento, max_completados FROM challenges;
$$;
