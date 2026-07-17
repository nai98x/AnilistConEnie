-- Cola de aprobación de vínculos de AniList (previo al vínculo, keyed por id de Discord; no FK).
CREATE TABLE IF NOT EXISTS anilist_approval (
    id_discord bigint  PRIMARY KEY,
    id_anilist integer NOT NULL,
    avatar     text    NOT NULL,
    site_url   text    NOT NULL,
    name       text    NOT NULL,
    banner     text    NOT NULL,
    message_id bigint  NOT NULL DEFAULT 0
);
