-- Setea (o limpia, si vienen NULL) el día y mes de cumpleaños del usuario. Si la fila no existe la
-- crea, para soportar el registro de cumple aunque el usuario aún no tenga otra data.
CREATE OR REPLACE FUNCTION usuario_set_cumple(
    p_user_id    bigint,
    p_cumple_dia smallint,
    p_cumple_mes smallint
) RETURNS void
LANGUAGE sql
AS $$
    INSERT INTO usuarios (user_id, cumple_dia, cumple_mes)
    VALUES (p_user_id, p_cumple_dia, p_cumple_mes)
    ON CONFLICT (user_id) DO UPDATE
        SET cumple_dia = EXCLUDED.cumple_dia,
            cumple_mes = EXCLUDED.cumple_mes;
$$;
