-- Historial diario para el chart de un usuario: el año actual completo (día a día) y los años
-- anteriores reducidos a 1 registro por mes (el más temprano), igual que el read viejo de Firestore.
-- La columna se devuelve como timestamp para mapear a UserDailyXp.Date (DateTime). Ordenado por fecha.
CREATE OR REPLACE FUNCTION xp_diario_chart(p_user_id bigint)
RETURNS TABLE(user_id bigint, "date" timestamp, xp bigint)
LANGUAGE sql
STABLE
AS $$
    SELECT user_id, fecha::timestamp, xp
    FROM xp_diario
    WHERE user_id = p_user_id
      AND date_part('year', fecha) = date_part('year', CURRENT_DATE)
    UNION ALL
    SELECT user_id, fecha::timestamp, xp
    FROM (
        SELECT DISTINCT ON (date_trunc('month', fecha)) user_id, fecha, xp
        FROM xp_diario
        WHERE user_id = p_user_id
          AND date_part('year', fecha) < date_part('year', CURRENT_DATE)
        ORDER BY date_trunc('month', fecha), fecha
    ) viejos
    ORDER BY 2;
$$;
