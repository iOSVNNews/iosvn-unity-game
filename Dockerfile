FROM node:22-alpine

WORKDIR /app
ENV NODE_ENV=production \
    IPA_HOST=0.0.0.0 \
    IPA_PORT=8788 \
    IPA_DATA_DIR=/data

COPY ipa_server.js email_auth_store.js ./
COPY ipa_core ./ipa_core

RUN mkdir -p /data && chown -R node:node /app /data
USER node

EXPOSE 8788
VOLUME ["/data"]
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD node -e "fetch('http://127.0.0.1:8788/health').then(r=>process.exit(r.ok?0:1)).catch(()=>process.exit(1))"

CMD ["node", "ipa_server.js"]
