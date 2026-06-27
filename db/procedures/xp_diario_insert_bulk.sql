-- Inserta de un saque todo el historial diario de un usuario (arrays paralelos fecha/xp).
-- Idempotente: si ya existe (user_id, fecha), actualiza el xp. Permite reanudar la migración.
CREATE OR REPLACE FUNCTION xp_diario_insert_bulk(
    p_user_id bigint,
    p_fechas  date[],
    p_xps     bigint[]
) RETURNS void
LANGUAGE sql
AS $$
    INSERT INTO xp_diario (user_id, fecha, xp)
    SELECT p_user_id, f, x
    FROM unnest(p_fechas, p_xps) AS t(f, x)
    ON CONFLICT (user_id, fecha) DO UPDATE SET xp = EXCLUDED.xp;
$$;
