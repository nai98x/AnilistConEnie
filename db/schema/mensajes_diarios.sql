-- Cantidad de mensajes por usuario, canal y día. Una fila por (usuario, canal, día); los mensajes en
-- hilos se cuentan en su canal padre.
-- Solo guarda la ventana de retención (MensajesDiarios:DiasRetencion): el cron diario purga el resto.
CREATE TABLE IF NOT EXISTS mensajes_diarios (
    user_id  bigint  NOT NULL REFERENCES usuarios (user_id),
    canal_id bigint  NOT NULL,
    fecha    date    NOT NULL,
    cantidad integer NOT NULL,
    PRIMARY KEY (user_id, canal_id, fecha)
);
