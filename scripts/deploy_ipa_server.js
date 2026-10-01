'use strict';

const fs = require('node:fs');
const path = require('node:path');
const { spawnSync } = require('node:child_process');
const root = path.resolve(__dirname, '..');
const key = process.env.IPA_SSH_KEY || 'D:/Bot_Chong_Spam_Telegram/LightsailDefaultKey-ap-southeast-1.pem';
const gameHost = '18.138.110.85';
const proxyHost = '175.41.171.227';
const publicOrigin = 'https://tutien.iosvn.com.vn';

function tool(command, args) {
    const result = spawnSync(command, args, { cwd: root, stdio: 'inherit', windowsHide: true });
    if (result.error) throw result.error;
    if (result.status !== 0) throw new Error(`${command} failed (${result.status})`);
}
const sshOptions = ['-i', key, '-o', 'IdentitiesOnly=yes', '-o', 'BatchMode=yes', '-o', 'ConnectTimeout=20'];
function ssh(host, command) { tool('ssh', [...sshOptions, `ec2-user@${host}`, command]); }
function upload(host, source, destination) { tool('scp', [...sshOptions, source, `ec2-user@${host}:${destination}`]); }
async function request(route, options = {}) {
    return fetch(publicOrigin + route, { ...options, signal: AbortSignal.timeout(15000) });
}

async function main() {
    const finishProxy = process.argv.includes('--finish-proxy');
    if (!process.argv.includes('--deploy') && !finishProxy) throw new Error('Use npm run deploy:ipa -- --deploy after reviewing the server changes.');
    if (!fs.existsSync(key)) throw new Error('Set IPA_SSH_KEY to the existing Lightsail private-key file.');
    const initialPage = await request('/');
    if (!initialPage.ok) throw new Error('Existing bot website is unavailable; refusing deployment.');
    ssh(gameHost, 'sudo -n systemctl is-active --quiet iosvn-reputation && node --version');
    ssh(proxyHost, 'sudo -n systemctl is-active --quiet caddy');
    if (!finishProxy) {
    tool('node', ['--test', 'tests/account_auth.test.js']);
    const release = new Date().toISOString().replace(/[^0-9]/g, '').slice(0, 14);
    const archive = path.join(root, 'build', 'ipa-server-deploy.tar.gz');
    fs.mkdirSync(path.dirname(archive), { recursive: true });
    tool('tar', ['-czf', archive, 'ipa_server.js', 'email_auth_store.js', 'gmail_mailer.js', 'account_oauth.js', 'package.json', 'package-lock.json', 'ipa_core', 'tests']);
    upload(gameHost, archive, '/tmp/iosvn-ipa-server.tar.gz');
    upload(gameHost, path.join(__dirname, 'ipa-server-install.sh'), '/tmp/iosvn-ipa-install.sh');
    upload(gameHost, path.join(__dirname, 'iosvn-ipa.service'), '/tmp/iosvn-ipa.service');
    ssh(gameHost, `sudo -n bash /tmp/iosvn-ipa-install.sh ${release}`);
    }
    upload(gameHost, path.join(__dirname, 'ipa-server-network.sh'), '/tmp/iosvn-ipa-network.sh');
    ssh(gameHost, 'sudo -n bash /tmp/iosvn-ipa-network.sh');
    ssh(proxyHost, 'curl -fsS --max-time 10 http://172.26.11.174:8788/health');
    upload(proxyHost, path.join(__dirname, 'configure_ipa_proxy.py'), '/tmp/configure_ipa_proxy.py');
    ssh(proxyHost, 'sudo -n python3 /tmp/configure_ipa_proxy.py');
    const health = await request('/ipa/health');
    if (!health.ok || (await health.json()).service !== 'iosvn-ipa-game-server') throw new Error('Public IPA health check failed.');
    const protectedState = await request('/ipa/api/state');
    if (protectedState.status !== 401) throw new Error('Unauthenticated game state was not protected.');
    const login = await request('/ipa/api/auth/account/login', {method:'POST',headers:{'Content-Type':'application/json'},
        body:JSON.stringify({identity:'nonexistent_deploy_check',password:'Not-a-real-account-password'})});
    if (login.status !== 401) throw new Error('Public account route check failed.');
    const page = await request('/');
    if (!page.ok) throw new Error('Bot website health check failed after adding IPA routing.');
    ssh(gameHost, 'sudo -n systemctl is-active --quiet iosvn-reputation && sudo -n systemctl is-active --quiet iosvn-ipa');
    console.log('LIVE_IPA_API_VERIFIED:', publicOrigin + '/ipa/api');
}
main().catch(error => { console.error(error.message); process.exitCode = 1; });
