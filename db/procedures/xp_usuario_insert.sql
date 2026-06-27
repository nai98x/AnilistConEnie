-- Crea la fila de XP del usuario. Falla si ya existe (usar xp_usuario_update para modificar).
CREATE OR REPLACE FUNCTION xp_usuario_insert(
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
    INSERT INTO xp_usuarios (user_id, total, booster, challenges, eventos, intercambios, otros)
    VALUES (p_user_id, p_total, p_booster, p_challenges, p_eventos, p_intercambios, p_otros);
$$;
