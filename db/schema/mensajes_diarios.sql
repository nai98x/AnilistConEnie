-- Cantidad de mensajes por usuario y día en el canal general. Una fila por (usuario, día).
-- Solo guarda la ventana de retención (MensajesDiarios:DiasRetencion): el cron diario purga el resto.
CREATE TABLE IF NOT EXISTS mensajes_diarios (
    user_id  bigint  NOT NULL REFERENCES usuarios (user_id),
    fecha    date    NOT NULL,
    cantidad integer NOT NULL,
    PRIMARY KEY (user_id, fecha)
);
