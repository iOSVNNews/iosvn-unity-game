#!/usr/bin/env bash
set -euo pipefail
if command -v firewall-cmd >/dev/null && firewall-cmd --state >/dev/null 2>&1; then
    IPA_PROXY_RULE='rule family="ipv4" source address="172.26.2.254/32" port port="8788" protocol="tcp" accept'
    firewall-cmd --permanent --add-rich-rule="$IPA_PROXY_RULE"
    firewall-cmd --add-rich-rule="$IPA_PROXY_RULE"
fi
