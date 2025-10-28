# ---- deps ----
FROM node:20-alpine AS deps
WORKDIR /app
# copie apenas os manifests para cache eficiente
COPY package.json package-lock.json* yarn.lock* pnpm-lock.yaml* ./
# instala com o gerenciador detectado
RUN if [ -f pnpm-lock.yaml ]; then corepack enable pnpm && pnpm i --frozen-lockfile; \
    elif [ -f yarn.lock ]; then yarn --frozen-lockfile; \
    else npm ci; fi

# ---- build ----
FROM node:20-alpine AS builder
WORKDIR /app

# se precisar que o valor entre no bundle do cliente, passe como build-arg
ARG NEXT_PUBLIC_API_URL
ENV NEXT_PUBLIC_API_URL=${NEXT_PUBLIC_API_URL}

COPY --from=deps /app/node_modules ./node_modules
COPY . .

# desabilita telemetria durante build
ENV NEXT_TELEMETRY_DISABLED=1

RUN if [ -f pnpm-lock.yaml ]; then corepack enable pnpm && pnpm run build; \
    elif [ -f yarn.lock ]; then yarn build; \
    else npm run build; fi

# ---- runner (standalone) ----
FROM node:20-alpine AS runner
WORKDIR /app
ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1

# copia artefatos "standalone" do Next para imagem mínima
COPY --from=builder /app/.next/standalone ./
COPY --from=builder /app/.next/static ./.next/static

# porta padrão do Next
EXPOSE 3000

# (opcional) healthcheck simples; remova se não quiser
HEALTHCHECK --interval=30s --timeout=5s --retries=5 CMD wget -qO- http://localhost:3000/ || exit 1

# roda como usuário não-root
RUN addgroup -S nextjs && adduser -S nextjs -G nextjs
USER nextjs

# o entrypoint do standalone é o server.js gerado pelo Next
CMD ["node", "server.js"]
