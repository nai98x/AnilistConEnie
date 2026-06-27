-- Crea o actualiza el mapeo de un mensaje de intercambio a su repost (por id del mensaje del hilo).
CREATE OR REPLACE FUNCTION intercambios_repost_upsert(
    p_id_mensaje_hilo_foro    bigint,
    p_id_canal_hilo_foro      bigint,
    p_id_canal_mensaje_repost bigint,
    p_id_mensaje_repost       bigint
) RETURNS void
LANGUAGE sql
AS $$
    INSERT INTO intercambios_repost (id_mensaje_hilo_foro, id_canal_hilo_foro, id_canal_mensaje_repost, id_mensaje_repost)
    VALUES (p_id_mensaje_hilo_foro, p_id_canal_hilo_foro, p_id_canal_mensaje_repost, p_id_mensaje_repost)
    ON CONFLICT (id_mensaje_hilo_foro) DO UPDATE
        SET id_canal_hilo_foro = EXCLUDED.id_canal_hilo_foro,
            id_canal_mensaje_repost = EXCLUDED.id_canal_mensaje_repost,
            id_mensaje_repost = EXCLUDED.id_mensaje_repost;
$$;
