-- Usuarios con actividad registrada posterior a la fecha indicada (los "activos").
CREATE OR REPLACE FUNCTION usuario_activos(p_desde timestamptz)
RETURNS TABLE(user_id bigint, last_activity timestamptz)
LANGUAGE sql
STABLE
AS $$
    SELECT user_id, last_activity
    FROM usuarios
    WHERE last_activity > p_desde;
$$;
