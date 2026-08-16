-- Completados por usuario. Una fila por (challenge, usuario). FK a challenges y a usuarios
-- (incluye completados de gente que ya no está en el server → la fila de usuarios existe igual).
-- completados: veces que el usuario completó el challenge; xp: XP acumulada por todas esas veces.
CREATE TABLE IF NOT EXISTS challenge_usuarios (
    challenge_id bigint      NOT NULL REFERENCES challenges (id),
    user_id      bigint      NOT NULL REFERENCES usuarios (user_id),
    xp           integer     NOT NULL,
    fecha        timestamptz NOT NULL,
    completados  integer     NOT NULL DEFAULT 1 CHECK (completados >= 1),
    PRIMARY KEY (challenge_id, user_id)
);
