-- Usuarios con fecha de entrada registrada (la primera vez que entraron al server; prevalece sobre el
-- joined_at de Discord, que se reinicia si salen y vuelven). No filtra por `activo`.
CREATE OR REPLACE FUNCTION usuario_fechas_entrada()
RETURNS TABLE(user_id bigint, fecha_entrada timestamptz)
LANGUAGE sql
STABLE
AS $$
    SELECT user_id, fecha_entrada
    FROM usuarios
    WHERE fecha_entrada IS NOT NULL;
$$;
