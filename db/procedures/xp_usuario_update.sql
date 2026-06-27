-- Actualiza (valores absolutos) la XP del usuario.
CREATE OR REPLACE FUNCTION xp_usuario_update(
    p_user_id      bigint,
    p_total        bigint,
    p_booster      bigint,
    p_challenges   bigint,
    p_eventos      bigint,
    p_intercambios bigint,
    p_otros        bigint
) RETURNS void
LANGUAGE sql
AS $$
    UPDATE xp_usuarios
    SET total = p_total,
        booster = p_booster,
        challenges = p_challenges,
        eventos = p_eventos,
        intercambios = p_intercambios,
        otros = p_otros
    WHERE user_id = p_user_id;
$$;
