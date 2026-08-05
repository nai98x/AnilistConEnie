#!/usr/bin/env bash
set -euo pipefail

CONF="${BACKUP_CONF:-$HOME/.config/anilist-backup/backup.env}"
[[ -f "$CONF" ]] || { echo "Falta el archivo de configuración: $CONF" >&2; exit 1; }
# shellcheck source=/dev/null
source "$CONF"

: "${PGDATABASE:?definir PGDATABASE en $CONF}"
: "${REMOTE:?definir REMOTE en $CONF}"

export PGDATABASE
export PGHOST="${PGHOST:-localhost}"
export PGPORT="${PGPORT:-5432}"
export PGUSER="${PGUSER:-backup_ro}"

RETENER="${RETENER:-5}"
PREFIJO="${PREFIJO:-backup}"
PASSFILE="${PASSFILE:-$HOME/.config/anilist-backup/passphrase}"
RCLONE="${RCLONE:-$HOME/bin/rclone}"
ESTADO="${ESTADO:-$HOME/.config/anilist-backup/ultimo-ok}"

TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

FECHA="$(TZ=America/Argentina/Buenos_Aires date +%Y-%m-%d)"
ARCH="$TMP/$PREFIJO-$FECHA.dump"

pg_dump --format=custom --compress=9 --file="$ARCH"

gpg --batch --yes --symmetric --cipher-algo AES256 --pinentry-mode loopback --passphrase-file "$PASSFILE" --output "$ARCH.gpg" "$ARCH"
ARCH="$ARCH.gpg"

"$RCLONE" copyto "$ARCH" "$REMOTE/$(basename "$ARCH")" --checksum

# Marca que lee el bot para avisar si un día no hubo backup. Se escribe recién acá: si algo falló
# antes, queda con la fecha vieja y el chequeo la detecta.
echo "$FECHA" > "$ESTADO"

# Retención: se dejan los $RETENER más nuevos (el nombre con la fecha ordena solo).
# Se poda después de subir, para que una racha de fallos no termine vaciando el remoto.
"$RCLONE" lsf "$REMOTE" --files-only | sort | head -n "-$RETENER" | while read -r f; do
    "$RCLONE" deletefile "$REMOTE/$f"
done
