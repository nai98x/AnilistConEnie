-- Crea o actualiza una entrada de la cola de aprobación (por id de Discord).
CREATE OR REPLACE FUNCTION anilist_approval_upsert(
    p_id_discord bigint,
    p_id_anilist integer,
    p_avatar     text,
    p_site_url   text,
    p_name       text,
    p_banner     text,
    p_message_id bigint
) RETURNS void
LANGUAGE sql
AS $$
    INSERT INTO anilist_approval (id_discord, id_anilist, avatar, site_url, name, banner, message_id)
    VALUES (p_id_discord, p_id_anilist, p_avatar, p_site_url, p_name, p_banner, p_message_id)
    ON CONFLICT (id_discord) DO UPDATE
        SET id_anilist = EXCLUDED.id_anilist,
            avatar = EXCLUDED.avatar,
            site_url = EXCLUDED.site_url,
            name = EXCLUDED.name,
            banner = EXCLUDED.banner,
            message_id = EXCLUDED.message_id;
$$;
