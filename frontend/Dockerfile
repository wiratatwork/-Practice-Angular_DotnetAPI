FROM node:22-alpine AS build
WORKDIR /app

COPY package*.json ./
COPY scripts/playwright-install.mjs scripts/
ENV PLAYWRIGHT_SKIP_INSTALL=1
RUN npm ci

COPY . .
RUN npm run build

FROM nginx:alpine
RUN apk add --no-cache curl
COPY --from=build /app/dist/frontend/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
