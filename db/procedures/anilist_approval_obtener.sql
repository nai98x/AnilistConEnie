-- Entrada de la cola de aprobación por id de Discord (0 o 1 fila).
CREATE OR REPLACE FUNCTION anilist_approval_obtener(p_id_discord bigint)
RETURNS SETOF anilist_approval
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM anilist_approval WHERE id_discord = p_id_discord;
$$;
