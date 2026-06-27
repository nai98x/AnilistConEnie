-- Quita una entrada de la cola de aprobación (aprobada o rechazada).
CREATE OR REPLACE FUNCTION anilist_approval_delete(p_id_discord bigint)
RETURNS void
LANGUAGE sql
AS $$
    DELETE FROM anilist_approval WHERE id_discord = p_id_discord;
$$;
