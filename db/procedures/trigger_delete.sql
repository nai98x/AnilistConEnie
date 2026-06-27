-- Borra un trigger por nombre. Devuelve true si existía (y se borró), false si no había nada.
CREATE OR REPLACE FUNCTION trigger_delete(p_nombre text)
RETURNS boolean
LANGUAGE sql
AS $$
    DELETE FROM triggers WHERE nombre = p_nombre RETURNING true;
$$;
