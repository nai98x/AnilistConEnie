-- Limpia el cumpleaños del usuario (funcionalidad del comando de borrar cumple).
CREATE OR REPLACE FUNCTION usuario_borrar_cumple(p_user_id bigint)
RETURNS void
LANGUAGE sql
AS $$
    UPDATE usuarios
    SET cumple_dia = NULL,
        cumple_mes = NULL
    WHERE user_id = p_user_id;
$$;
