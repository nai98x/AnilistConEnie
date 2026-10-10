-- Por usuario, total de mensajes y cantidad de días distintos con mensajes en [p_desde, p_hasta].
CREATE OR REPLACE FUNCTION mensajes_diarios_totales(
    p_desde date,
    p_hasta date
) RETURNS TABLE (user_id bigint, total bigint, dias bigint)
LANGUAGE sql
AS $$
    SELECT m.user_id, SUM(m.cantidad), COUNT(*)
    FROM mensajes_diarios m
    WHERE m.fecha BETWEEN p_desde AND p_hasta
    GROUP BY m.user_id;
$$;
