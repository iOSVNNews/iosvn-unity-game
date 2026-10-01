#!/usr/bin/env bash
set -euo pipefail
RELEASE_ID="$1"
[[ "$RELEASE_ID" =~ ^[0-9]{14}$ ]] || { echo 'Invalid release ID' >&2; exit 1; }
node -e 'if (Number(process.versions.node.split(".")[0]) < 22) process.exit(1)'
systemctl is-active --quiet iosvn-reputation
CODE_ROOT=/opt/iosvn-ipa
RELEASE_PATH="$CODE_ROOT/releases/$RELEASE_ID"
DATA_PATH=/var/lib/iosvn-ipa
if [[ -e "$CODE_ROOT/current" && ! -L "$CODE_ROOT/current" ]]; then echo 'Expected current to be a release symlink' >&2; exit 1; fi
id iosvn-ipa >/dev/null 2>&1 || useradd --system --home-dir "$DATA_PATH" --shell /usr/sbin/nologin iosvn-ipa
install -d -o iosvn-ipa -g iosvn-ipa -m 750 "$DATA_PATH" "$DATA_PATH/npm-cache" "$RELEASE_PATH"
tar -xzf /tmp/iosvn-ipa-server.tar.gz -C "$RELEASE_PATH"
chown -R iosvn-ipa:iosvn-ipa "$RELEASE_PATH"
cd "$RELEASE_PATH"
sudo -u iosvn-ipa npm ci --omit=dev --cache "$DATA_PATH/npm-cache" --no-audit
sudo -u iosvn-ipa node --test tests/account_auth.test.js
if [[ ! -e /etc/iosvn-ipa.env ]]; then
    printf '%s\n' 'IPA_HOST=172.26.11.174' 'IPA_PORT=8788' 'IPA_DATA_DIR=/var/lib/iosvn-ipa' 'IPA_PUBLIC_API_URL=https://tutien.iosvn.com.vn/ipa/api' > /etc/iosvn-ipa.env
    chmod 600 /etc/iosvn-ipa.env
fi
PREVIOUS_RELEASE="$(readlink "$CODE_ROOT/current" || true)"
install -m 644 /tmp/iosvn-ipa.service /etc/systemd/system/iosvn-ipa.service
ln -sfn "$RELEASE_PATH" "$CODE_ROOT/current"
systemctl daemon-reload
systemctl enable iosvn-ipa
systemctl restart iosvn-ipa
READY=false
for attempt in $(seq 1 20); do
    if curl -fsS --max-time 2 http://172.26.11.174:8788/health >/dev/null; then READY=true; break; fi
    sleep 1
done
if [[ "$READY" != true ]]; then
    if [[ -n "$PREVIOUS_RELEASE" ]]; then ln -sfn "$PREVIOUS_RELEASE" "$CODE_ROOT/current"; systemctl restart iosvn-ipa; else systemctl stop iosvn-ipa; fi
    echo 'IPA service did not become healthy; restored its previous release when available.' >&2
    exit 1
fi
systemctl is-active --quiet iosvn-reputation
echo "IPA service healthy: $RELEASE_ID"
