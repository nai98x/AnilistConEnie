-- Cooldown Teiou vigente de un usuario (NULL si no tiene).
CREATE OR REPLACE FUNCTION teiou_cooldown_obtener(p_user_id bigint)
RETURNS timestamptz
LANGUAGE sql
STABLE
AS $$
    SELECT cooldown FROM teiou_cooldown WHERE user_id = p_user_id;
$$;
