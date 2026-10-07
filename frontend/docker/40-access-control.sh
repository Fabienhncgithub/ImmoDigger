#!/bin/sh
# Turns APP_USERS / APP_READONLY_USERS into nginx basic-auth files at start-up.
#
#   APP_USERS="alice:a-long-password,bob:another-one"      full access
#   APP_READONLY_USERS="carol:read-only-password"          can browse, cannot change anything
#
# Entries are separated by commas, so a password may contain ":" but not ",".
# Without APP_USERS the site is served without a login: fine on localhost,
# never acceptable on a public address (compose.prod.yaml refuses to start).
set -eu

access_dir=/etc/nginx/access
mkdir -p "$access_dir"
: > "$access_dir/server.conf"
: > "$access_dir/write.conf"

add_users() {
    # $1 = "name:password,..."  $2... = htpasswd files to append to
    list=$1
    shift
    old_ifs=$IFS
    IFS=,
    set -f
    for entry in $list; do
        [ -n "$entry" ] || continue
        name=${entry%%:*}
        password=${entry#*:}
        if [ "$name" = "$entry" ] || [ -z "$name" ] || [ -z "$password" ]; then
            echo "access-control: every entry must look like name:password" >&2
            exit 1
        fi
        hash=$(printf '%s' "$password" | openssl passwd -apr1 -stdin)
        for file in "$@"; do
            printf '%s:%s\n' "$name" "$hash" >> "$file"
        done
    done
    set +f
    IFS=$old_ifs
}

if [ -z "${APP_USERS:-}" ]; then
    if [ -n "${APP_READONLY_USERS:-}" ]; then
        echo "access-control: APP_READONLY_USERS needs APP_USERS to be set as well" >&2
        exit 1
    fi
    echo "access-control: WARNING - APP_USERS is empty, the site is served WITHOUT a login." >&2
    exit 0
fi

: > "$access_dir/everyone.htpasswd"
: > "$access_dir/editors.htpasswd"
add_users "$APP_USERS" "$access_dir/everyone.htpasswd" "$access_dir/editors.htpasswd"

cat > "$access_dir/server.conf" <<CONF
auth_basic "ImmoDigger";
auth_basic_user_file $access_dir/everyone.htpasswd;
CONF

if [ -n "${APP_READONLY_USERS:-}" ]; then
    add_users "$APP_READONLY_USERS" "$access_dir/everyone.htpasswd"
    # Anything other than reading the API is reserved to the full-access users.
    cat > "$access_dir/write.conf" <<CONF
limit_except GET HEAD {
    auth_basic "ImmoDigger";
    auth_basic_user_file $access_dir/editors.htpasswd;
}
CONF
fi

chmod 640 "$access_dir"/*.htpasswd
chgrp nginx "$access_dir"/*.htpasswd
echo "access-control: login required ($(wc -l < "$access_dir/everyone.htpasswd") user(s))."
