-- Todos los challenges del servidor (maestro).
CREATE OR REPLACE FUNCTION challenge_lista()
RETURNS TABLE(nombre text, link text, disponible boolean, vencimiento timestamptz)
LANGUAGE sql
STABLE
AS $$
    SELECT nombre, link, disponible, vencimiento FROM challenges;
$$;
