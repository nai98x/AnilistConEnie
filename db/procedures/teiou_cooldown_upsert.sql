-- Crea o actualiza el cooldown Teiou de un usuario.
CREATE OR REPLACE FUNCTION teiou_cooldown_upsert(
    p_user_id  bigint,
    p_cooldown timestamptz
) RETURNS void
LANGUAGE sql
AS $$
    INSERT INTO teiou_cooldown (user_id, cooldown)
    VALUES (p_user_id, p_cooldown)
    ON CONFLICT (user_id) DO UPDATE
        SET cooldown = EXCLUDED.cooldown;
$$;
