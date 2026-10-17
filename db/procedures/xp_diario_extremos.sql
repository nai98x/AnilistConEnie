-- Primer y último registro del historial diario de cada usuario (para el promedio de XP por día de
-- todo el servidor en una sola consulta). Las fechas se devuelven como timestamp para mapear a DateTime.
CREATE OR REPLACE FUNCTION xp_diario_extremos()
RETURNS TABLE(user_id bigint, primera_fecha timestamp, primer_xp bigint, ultima_fecha timestamp, ultimo_xp bigint)
LANGUAGE sql
STABLE
AS $$
    WITH primeros AS (
        SELECT DISTINCT ON (user_id) user_id, fecha, xp
        FROM xp_diario
        ORDER BY user_id, fecha
    ), ultimos AS (
        SELECT DISTINCT ON (user_id) user_id, fecha, xp
        FROM xp_diario
        ORDER BY user_id, fecha DESC
    )
    SELECT p.user_id, p.fecha::timestamp, p.xp, u.fecha::timestamp, u.xp
    FROM primeros p
    JOIN ultimos u ON u.user_id = p.user_id;
$$;
