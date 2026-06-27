-- IDs de usuarios de AniList baneados (no son del server, no FK a usuarios).
CREATE TABLE IF NOT EXISTS anilist_baneados (
    anilist_user_id integer PRIMARY KEY
);
