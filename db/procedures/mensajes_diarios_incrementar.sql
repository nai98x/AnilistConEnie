-- Suma un mensaje al conteo de un (usuario, canal, día); si no había fila, la crea en 1.
CREATE OR REPLACE FUNCTION mensajes_diarios_incrementar(
    p_user_id  bigint,
    p_canal_id bigint,
    p_fecha    date
) RETURNS void
LANGUAGE sql
AS $$
    INSERT INTO mensajes_diarios (user_id, canal_id, fecha, cantidad)
    VALUES (p_user_id, p_canal_id, p_fecha, 1)
    ON CONFLICT (user_id, canal_id, fecha) DO UPDATE SET cantidad = mensajes_diarios.cantidad + 1;
$$;
