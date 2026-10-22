FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json ./

RUN npm config set fetch-retry-maxtimeout 600000 \
    && npm config set fetch-timeout 600000 \
    && npm ci

COPY . .
RUN npm run build

FROM nginx:1.27-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
RUN addgroup -S coachpulse && adduser -S -G coachpulse -h /var/cache/nginx coachpulse \
    && chown -R coachpulse:coachpulse /usr/share/nginx/html /var/cache/nginx /var/log/nginx /etc/nginx/conf.d \
    && touch /var/run/nginx.pid && chown coachpulse:coachpulse /var/run/nginx.pid
EXPOSE 8081
USER coachpulse
HEALTHCHECK --interval=10s --timeout=3s --retries=5 CMD wget --quiet --tries=1 --spider http://127.0.0.1:8081/healthz || exit 1
CMD ["nginx", "-g", "daemon off;"]