"""Add a dedicated IPA prefix without changing the existing bot upstream."""
from pathlib import Path
import re
import shutil
import subprocess
import time

def add_route(source):
    start = re.search(r'(?m)^tutien\.iosvn\.com\.vn[^\n]*\{', source)
    if not start:
        raise RuntimeError('Expected Tu Tien Caddy site is missing')
    opened = source.index('{', start.start())
    depth = 1
    end = opened + 1
    while end < len(source) and depth:
        if source[end] == '{': depth += 1
        elif source[end] == '}': depth -= 1
        end += 1
    if depth:
        raise RuntimeError('Unbalanced Caddy site block')
    site = source[start.start():end]
    if 'handle_path /ipa/*' in site:
        if 'reverse_proxy 172.26.11.174:8788' not in site:
            raise RuntimeError('Existing IPA route has an unexpected upstream')
        return source
    upstream = re.compile(r'(?m)^(\s*)reverse_proxy 172\.26\.11\.174:8787\s*$')
    if len(upstream.findall(site)) != 1:
        raise RuntimeError('Expected exactly one existing game upstream')
    replacement = '\thandle_path /ipa/* {\n\t\treverse_proxy 172.26.11.174:8788\n\t}\n\thandle {\n\t\treverse_proxy 172.26.11.174:8787\n\t}'
    return source[:start.start()] + upstream.sub(lambda _: replacement, site) + source[end:]

def main():
    target = Path('/etc/caddy/Caddyfile')
    original = target.read_text()
    updated = add_route(original)
    if updated == original:
        print('IPA proxy already configured')
        return
    candidate = target.with_name('Caddyfile.ipa-candidate')
    candidate.write_text(updated)
    subprocess.run(['caddy', 'validate', '--config', str(candidate), '--adapter', 'caddyfile'], check=True)
    backup = target.with_name('Caddyfile.before-ipa-' + str(int(time.time())))
    shutil.copy2(target, backup)
    shutil.copyfile(candidate, target)
    try:
        subprocess.run(['systemctl', 'reload', 'caddy'], check=True)
    except Exception:
        shutil.copyfile(backup, target)
        subprocess.run(['systemctl', 'reload', 'caddy'], check=True)
        raise
    print('IPA prefix added; existing game upstream preserved')

if __name__ == '__main__': main()
