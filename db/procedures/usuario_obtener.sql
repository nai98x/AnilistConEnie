-- Devuelve el usuario por su ID de Discord (0 o 1 fila). No filtra por `activo`: un usuario dado de
-- baja sigue siendo consultable (trazabilidad).
CREATE OR REPLACE FUNCTION usuario_obtener(p_user_id bigint)
RETURNS SETOF usuarios
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM usuarios WHERE user_id = p_user_id;
$$;
