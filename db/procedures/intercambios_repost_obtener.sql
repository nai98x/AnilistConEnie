-- Mapeo de un mensaje de intercambio a su repost, por el id del mensaje del hilo/foro (0 o 1 fila).
CREATE OR REPLACE FUNCTION intercambios_repost_obtener(p_id_mensaje_hilo_foro bigint)
RETURNS SETOF intercambios_repost
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM intercambios_repost WHERE id_mensaje_hilo_foro = p_id_mensaje_hilo_foro;
$$;
