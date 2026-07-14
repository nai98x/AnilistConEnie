-- Mapeo del mensaje del hilo/foro de intercambios a sus reposts. Un post con varios tags se
-- repostea a varios canales: una fila por repost. Key = (mensaje del hilo, canal del repost).
CREATE TABLE IF NOT EXISTS intercambios_repost (
    id_mensaje_hilo_foro    bigint NOT NULL,
    id_canal_hilo_foro      bigint NOT NULL,
    id_canal_mensaje_repost bigint NOT NULL,
    id_mensaje_repost       bigint NOT NULL,
    PRIMARY KEY (id_mensaje_hilo_foro, id_canal_mensaje_repost)
);
