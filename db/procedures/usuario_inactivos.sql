-- Usuarios cuya última actividad es anterior a la fecha indicada (los "inactivos"). Los que nunca
-- tuvieron actividad (last_activity NULL) no se devuelven, igual que el read viejo de Firestore.
CREATE OR REPLACE FUNCTION usuario_inactivos(p_hasta timestamptz)
RETURNS TABLE(user_id bigint, last_activity timestamptz)
LANGUAGE sql
STABLE
AS $$
    SELECT user_id, last_activity
    FROM usuarios
    WHERE last_activity < p_hasta;
$$;
