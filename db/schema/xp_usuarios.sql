-- XP actual de cada usuario, desglosada por categoría. Una fila por usuario.
-- FK a usuarios: como la baja es lógica, la fila destino siempre existe (no hay cascada).
CREATE TABLE IF NOT EXISTS xp_usuarios (
    user_id      bigint PRIMARY KEY REFERENCES usuarios (user_id),
    total        bigint NOT NULL DEFAULT 0,
    booster      bigint NOT NULL DEFAULT 0,
    challenges   bigint NOT NULL DEFAULT 0,
    eventos      bigint NOT NULL DEFAULT 0,
    intercambios bigint NOT NULL DEFAULT 0,
    otros        bigint NOT NULL DEFAULT 0
);
