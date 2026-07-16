-- Transfiere la xp de un usuario a otro (xp actual + historial diario) en una sola transacción.
-- El origen queda intacto: es una copia, no un movimiento.
-- p_reemplazar = true: el destino queda igual al origen (se pisan su xp y su historial; si el origen
-- no tiene xp, el destino queda en cero).
-- p_reemplazar = false: se suma categoría por categoría, y en el historial los días que colisionan
-- se suman.
-- Devuelve la xp del destino ya transferida.
CREATE OR REPLACE FUNCTION xp_transferir(
    p_origen     bigint,
    p_destino    bigint,
    p_reemplazar boolean
) RETURNS SETOF xp_usuarios
LANGUAGE plpgsql
AS $$
BEGIN
    IF p_origen = p_destino THEN
        RAISE EXCEPTION 'El origen y el destino son el mismo usuario (%)', p_origen;
    END IF;

    PERFORM usuario_asegurar(p_destino);

    IF p_reemplazar THEN
        DELETE FROM xp_diario WHERE user_id = p_destino;

        INSERT INTO xp_diario (user_id, fecha, xp)
        SELECT p_destino, fecha, xp
        FROM xp_diario
        WHERE user_id = p_origen;

        INSERT INTO xp_usuarios (user_id, total, booster, challenges, eventos, intercambios, otros)
        SELECT p_destino, COALESCE(o.total, 0), COALESCE(o.booster, 0), COALESCE(o.challenges, 0),
               COALESCE(o.eventos, 0), COALESCE(o.intercambios, 0), COALESCE(o.otros, 0)
        FROM (SELECT 1) AS forzar_fila
        LEFT JOIN xp_usuarios o ON o.user_id = p_origen
        ON CONFLICT (user_id) DO UPDATE
            SET total = EXCLUDED.total,
                booster = EXCLUDED.booster,
                challenges = EXCLUDED.challenges,
                eventos = EXCLUDED.eventos,
                intercambios = EXCLUDED.intercambios,
                otros = EXCLUDED.otros;
    ELSE
        INSERT INTO xp_diario (user_id, fecha, xp)
        SELECT p_destino, fecha, xp
        FROM xp_diario
        WHERE user_id = p_origen
        ON CONFLICT (user_id, fecha) DO UPDATE SET xp = xp_diario.xp + EXCLUDED.xp;

        INSERT INTO xp_usuarios (user_id, total, booster, challenges, eventos, intercambios, otros)
        SELECT p_destino, o.total, o.booster, o.challenges, o.eventos, o.intercambios, o.otros
        FROM xp_usuarios o
        WHERE o.user_id = p_origen
        ON CONFLICT (user_id) DO UPDATE
            SET total = xp_usuarios.total + EXCLUDED.total,
                booster = xp_usuarios.booster + EXCLUDED.booster,
                challenges = xp_usuarios.challenges + EXCLUDED.challenges,
                eventos = xp_usuarios.eventos + EXCLUDED.eventos,
                intercambios = xp_usuarios.intercambios + EXCLUDED.intercambios,
                otros = xp_usuarios.otros + EXCLUDED.otros;
    END IF;

    RETURN QUERY SELECT * FROM xp_usuarios WHERE user_id = p_destino;
END;
$$;
