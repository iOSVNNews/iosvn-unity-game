'use strict';

const nodemailer = require('nodemailer');

function createGmailMailer(env = process.env) {
    const user = String(env.GMAIL_SMTP_USER || '').trim();
    const appPassword = String(env.GMAIL_SMTP_APP_PASSWORD || '').replace(/\s/g, '');
    const fromName = String(env.GMAIL_FROM_NAME || 'Tu Tiên Giới').trim() || 'Tu Tiên Giới';
    const configured = Boolean(user && appPassword);
    let transporter = null;

    function requireTransporter() {
        if (!configured) {
            const error = new Error('Máy chủ chưa cấu hình Gmail để gửi mã xác minh.');
            error.status = 503;
            error.code = 'email_delivery_not_configured';
            throw error;
        }
        if (!transporter) {
            transporter = nodemailer.createTransport({
                service: 'gmail',
                auth: { user, pass: appPassword },
                pool: true,
                maxConnections: 2,
                maxMessages: 20,
                connectionTimeout: 10000,
                greetingTimeout: 10000,
                socketTimeout: 15000,
            });
        }
        return transporter;
    }

    return {
        isConfigured: () => configured,
        async sendVerificationCode(to, code) {
            const transport = requireTransporter();
            await transport.sendMail({
                from: { name: fromName, address: user },
                to,
                subject: 'Mã xác minh tài khoản Tu Tiên Giới',
                text: `Mã xác minh của bạn là: ${code}\nMã có hiệu lực trong 10 phút. Nếu bạn không yêu cầu tạo tài khoản, hãy bỏ qua email này.`,
                html: `<div style="font-family:Arial,sans-serif;max-width:520px;margin:auto;color:#20242a"><h2>Tu Tiên Giới</h2><p>Mã xác minh tài khoản của bạn:</p><p style="font-size:30px;font-weight:bold;letter-spacing:8px;color:#9b6a1b">${code}</p><p>Mã có hiệu lực trong 10 phút. Nếu bạn không yêu cầu tạo tài khoản, hãy bỏ qua email này.</p></div>`,
            });
        },
        close() { transporter?.close(); },
    };
}

module.exports = { createGmailMailer };
