-- Borrado lógico: marca al usuario como inactivo sin borrar la fila ni datos relacionados.
CREATE OR REPLACE FUNCTION usuario_desactivar(p_user_id bigint)
RETURNS void
LANGUAGE sql
AS $$
    UPDATE usuarios SET activo = false WHERE user_id = p_user_id;
$$;
