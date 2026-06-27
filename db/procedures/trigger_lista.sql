-- Todos los triggers, ordenados por nombre.
CREATE OR REPLACE FUNCTION trigger_lista()
RETURNS TABLE(nombre text, texto text, image_url text, tipo integer)
LANGUAGE sql
STABLE
AS $$
    SELECT nombre, texto, image_url, tipo FROM triggers ORDER BY nombre;
$$;
