-- Completa la fecha de entrada de los usuarios que no la tienen registrada (arrays paralelos
-- user_id/fecha, con el joined_at de Discord). Nunca pisa una fecha ya registrada. Los IDs sin fila
-- en `usuarios` se ignoran.
CREATE OR REPLACE FUNCTION usuario_completar_fechas_entrada(
    p_user_ids bigint[],
    p_fechas   timestamptz[]
) RETURNS integer
LANGUAGE sql
AS $$
    WITH actualizados AS (
        UPDATE usuarios u
        SET fecha_entrada = t.f
        FROM unnest(p_user_ids, p_fechas) AS t(id, f)
        WHERE u.user_id = t.id
          AND u.fecha_entrada IS NULL
        RETURNING 1
    )
    SELECT count(*)::integer FROM actualizados;
$$;
