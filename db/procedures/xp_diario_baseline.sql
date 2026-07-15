-- Baseline de XP por usuario para los tops parciales: el registro más reciente de cada usuario
-- con fecha <= p_fecha. El snapshot diario se toma a las 00:00 ART con la fecha del día que
-- empieza, así que el registro de fecha D es el acumulado al inicio del día D.
CREATE OR REPLACE FUNCTION xp_diario_baseline(p_fecha date)
RETURNS TABLE(user_id bigint, "date" timestamp, xp bigint)
LANGUAGE sql
STABLE
AS $$
    SELECT DISTINCT ON (user_id) user_id, fecha::timestamp, xp
    FROM xp_diario
    WHERE fecha <= p_fecha
    ORDER BY user_id, fecha DESC;
$$;
