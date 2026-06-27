-- Inserta o actualiza la XP de un (usuario, día). Para el registro diario y el relleno de huecos.
CREATE OR REPLACE FUNCTION xp_diario_upsert(
    p_user_id bigint,
    p_fecha   date,
    p_xp      bigint
) RETURNS void
LANGUAGE sql
AS $$
    INSERT INTO xp_diario (user_id, fecha, xp)
    VALUES (p_user_id, p_fecha, p_xp)
    ON CONFLICT (user_id, fecha) DO UPDATE SET xp = EXCLUDED.xp;
$$;
