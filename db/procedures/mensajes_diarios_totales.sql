-- Por usuario, en [p_desde, p_hasta]: total de mensajes (todos los canales), cantidad de días distintos con
-- mensajes y la mayor cantidad de mensajes en un solo canal sin contar p_canal_excluido (0 si no hay otros).
CREATE OR REPLACE FUNCTION mensajes_diarios_totales(
    p_desde          date,
    p_hasta          date,
    p_canal_excluido bigint
) RETURNS TABLE (user_id bigint, total bigint, dias bigint, maximo_en_un_canal bigint)
LANGUAGE sql
AS $$
    WITH por_canal AS (
        SELECT m.user_id, m.canal_id, SUM(m.cantidad) AS cantidad
        FROM mensajes_diarios m
        WHERE m.fecha BETWEEN p_desde AND p_hasta
        GROUP BY m.user_id, m.canal_id
    ), por_dias AS (
        SELECT m.user_id, COUNT(DISTINCT m.fecha) AS dias
        FROM mensajes_diarios m
        WHERE m.fecha BETWEEN p_desde AND p_hasta
        GROUP BY m.user_id
    )
    SELECT c.user_id,
           SUM(c.cantidad)::bigint,
           d.dias,
           COALESCE(MAX(c.cantidad) FILTER (WHERE c.canal_id <> p_canal_excluido), 0)::bigint
    FROM por_canal c
    JOIN por_dias d ON d.user_id = c.user_id
    GROUP BY c.user_id, d.dias;
$$;
