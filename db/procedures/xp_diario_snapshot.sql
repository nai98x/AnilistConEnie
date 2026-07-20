-- Inserta el snapshot de XP de un día para muchos usuarios de un saque (arrays paralelos user_id/xp).
-- Idempotente: si ya existe (user_id, fecha), actualiza el xp. Es el registro diario del ranking.
CREATE OR REPLACE FUNCTION xp_diario_snapshot(
    p_fecha    date,
    p_user_ids bigint[],
    p_xps      bigint[]
) RETURNS void
LANGUAGE sql
AS $$
    INSERT INTO xp_diario (user_id, fecha, xp)
    SELECT u, p_fecha, x
    FROM unnest(p_user_ids, p_xps) AS t(u, x)
    ON CONFLICT (user_id, fecha) DO UPDATE SET xp = EXCLUDED.xp;
$$;
