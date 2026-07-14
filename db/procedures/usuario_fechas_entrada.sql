-- Usuarios con fecha de entrada real registrada (excepciones al joined_at de Discord, usadas para
-- fundadores y aniversarios). No filtra por `activo`: la excepción vale aunque el usuario se haya ido.
CREATE OR REPLACE FUNCTION usuario_fechas_entrada()
RETURNS TABLE(user_id bigint, fecha_entrada timestamptz)
LANGUAGE sql
STABLE
AS $$
    SELECT user_id, fecha_entrada
    FROM usuarios
    WHERE fecha_entrada IS NOT NULL;
$$;
