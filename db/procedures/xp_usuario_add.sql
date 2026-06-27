-- Suma (p_sign=1) o resta (p_sign=-1) XP por categoría de forma atómica. Si el usuario no tenía fila,
-- la crea con el delta aplicado. Reemplaza el read-modify-write que se hacía contra Firestore.
CREATE OR REPLACE FUNCTION xp_usuario_add(
    p_user_id      bigint,
    p_total        bigint,
    p_booster      bigint,
    p_challenges   bigint,
    p_eventos      bigint,
    p_intercambios bigint,
    p_otros        bigint,
    p_sign         integer
) RETURNS void
LANGUAGE sql
AS $$
    INSERT INTO xp_usuarios (user_id, total, booster, challenges, eventos, intercambios, otros)
    VALUES (p_user_id, p_sign * p_total, p_sign * p_booster, p_sign * p_challenges,
            p_sign * p_eventos, p_sign * p_intercambios, p_sign * p_otros)
    ON CONFLICT (user_id) DO UPDATE
        SET total = xp_usuarios.total + p_sign * p_total,
            booster = xp_usuarios.booster + p_sign * p_booster,
            challenges = xp_usuarios.challenges + p_sign * p_challenges,
            eventos = xp_usuarios.eventos + p_sign * p_eventos,
            intercambios = xp_usuarios.intercambios + p_sign * p_intercambios,
            otros = xp_usuarios.otros + p_sign * p_otros;
$$;
