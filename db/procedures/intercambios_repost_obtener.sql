-- Reposts de un mensaje de intercambio, por el id del mensaje del hilo/foro (una fila por canal).
CREATE OR REPLACE FUNCTION intercambios_repost_obtener(p_id_mensaje_hilo_foro bigint)
RETURNS SETOF intercambios_repost
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM intercambios_repost WHERE id_mensaje_hilo_foro = p_id_mensaje_hilo_foro;
$$;
