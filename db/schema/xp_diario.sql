-- Historial diario de XP por usuario (alimenta los charts). Una fila por (usuario, día).
-- PK compuesta (user_id, fecha): cubre el acceso típico del chart (un usuario ordenado por fecha)
-- sin índices extra, y da idempotencia en la migración vía ON CONFLICT.
CREATE TABLE IF NOT EXISTS xp_diario (
    user_id bigint NOT NULL REFERENCES usuarios (user_id),
    fecha   date   NOT NULL,
    xp      bigint NOT NULL,
    PRIMARY KEY (user_id, fecha)
);
