-- Mapeo del mensaje del hilo/foro de intercambios a su repost. Key = id del mensaje del hilo.
CREATE TABLE IF NOT EXISTS intercambios_repost (
    id_mensaje_hilo_foro    bigint PRIMARY KEY,
    id_canal_hilo_foro      bigint NOT NULL,
    id_canal_mensaje_repost bigint NOT NULL,
    id_mensaje_repost       bigint NOT NULL
);
