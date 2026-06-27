-- Actualiza la última actividad registrada del usuario.
CREATE OR REPLACE FUNCTION usuario_set_last_activity(
    p_user_id       bigint,
    p_last_activity timestamptz
) RETURNS void
LANGUAGE sql
AS $$
    UPDATE usuarios
    SET last_activity = p_last_activity
    WHERE user_id = p_user_id;
$$;
