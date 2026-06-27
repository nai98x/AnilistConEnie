-- Día y mes de cumpleaños de los usuarios activos que lo tienen registrado.
CREATE OR REPLACE FUNCTION usuario_cumples()
RETURNS TABLE(user_id bigint, cumple_dia smallint, cumple_mes smallint)
LANGUAGE sql
STABLE
AS $$
    SELECT user_id, cumple_dia, cumple_mes
    FROM usuarios
    WHERE activo AND cumple_dia IS NOT NULL;
$$;
