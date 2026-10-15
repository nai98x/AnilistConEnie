-- Fija la fecha de entrada real del usuario, pisando la registrada (corrección manual de owner para
-- quienes entraron antes de que se persistiera). Solo actualiza: devuelve false si no hay fila.
CREATE OR REPLACE FUNCTION usuario_set_fecha_entrada(
    p_user_id       bigint,
    p_fecha_entrada timestamptz
) RETURNS boolean
LANGUAGE sql
AS $$
    WITH actualizados AS (
        UPDATE usuarios
        SET fecha_entrada = p_fecha_entrada
        WHERE user_id = p_user_id
        RETURNING 1
    )
    SELECT EXISTS (SELECT 1 FROM actualizados);
$$;
