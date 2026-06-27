-- Cooldown del nickname Teiou por usuario (efímero, sin FK).
CREATE TABLE IF NOT EXISTS teiou_cooldown (
    user_id  bigint      PRIMARY KEY,
    cooldown timestamptz NOT NULL
);
