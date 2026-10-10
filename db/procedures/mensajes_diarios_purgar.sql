-- Borra los conteos anteriores a p_fecha_minima (primer día que se conserva de la ventana).
CREATE OR REPLACE FUNCTION mensajes_diarios_purgar(
    p_fecha_minima date
) RETURNS void
LANGUAGE sql
AS $$
    DELETE FROM mensajes_diarios WHERE fecha < p_fecha_minima;
$$;
