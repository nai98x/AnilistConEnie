-- Asegura que exista la fila del usuario sin pisar datos: si no existe, la crea como baja
-- (activo=false, sin vínculo de AniList). Para migrar usuarios que ya no están vinculados pero
-- conservan historial (XP, cumple, actividad), de modo que el FK desde esas tablas tenga destino.
CREATE OR REPLACE FUNCTION usuario_asegurar(p_user_id bigint)
RETURNS void
LANGUAGE sql
AS $$
    INSERT INTO usuarios (user_id, activo)
    VALUES (p_user_id, false)
    ON CONFLICT (user_id) DO NOTHING;
$$;
