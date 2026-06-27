-- Borra el mapeo de un mensaje de intercambio (cuando se borra el mensaje del hilo).
CREATE OR REPLACE FUNCTION intercambios_repost_delete(p_id_mensaje_hilo_foro bigint)
RETURNS void
LANGUAGE sql
AS $$
    DELETE FROM intercambios_repost WHERE id_mensaje_hilo_foro = p_id_mensaje_hilo_foro;
$$;
