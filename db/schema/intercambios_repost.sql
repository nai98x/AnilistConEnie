-- Mapeo del mensaje del hilo/foro de intercambios a sus reposts. Un post con varios tags se
-- repostea a varios canales: una fila por repost. Key = (mensaje del hilo, canal del repost).
CREATE TABLE IF NOT EXISTS intercambios_repost (
    id_mensaje_hilo_foro    bigint NOT NULL,
    id_canal_hilo_foro      bigint NOT NULL,
    id_canal_mensaje_repost bigint NOT NULL,
    id_mensaje_repost       bigint NOT NULL,
    PRIMARY KEY (id_mensaje_hilo_foro, id_canal_mensaje_repost)
);

-- Migración desde la PK original (solo id_mensaje_hilo_foro), donde un post con varios tags pisaba
-- el registro de sus otros reposts.
DO $$
BEGIN
    IF (SELECT count(*) FROM information_schema.key_column_usage
        WHERE table_name = 'intercambios_repost' AND constraint_name = 'intercambios_repost_pkey') = 1
    THEN
        ALTER TABLE intercambios_repost DROP CONSTRAINT intercambios_repost_pkey;
        ALTER TABLE intercambios_repost ADD PRIMARY KEY (id_mensaje_hilo_foro, id_canal_mensaje_repost);
    END IF;
END $$;
