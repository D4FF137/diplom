# Stage 1: Build the React application
FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm install --legacy-peer-deps
COPY . .

# Vite вшивает VITE_* переменные в бандл во время `npm run build`,
# поэтому они должны быть доступны как build-args, а не runtime env.
ARG VITE_API_URL=http://localhost/api
ARG VITE_WS_URL=http://localhost
ARG VITE_FEED_WS_URL=http://localhost
ARG VITE_NOTIFICATION_WS_URL=http://localhost
ARG VITE_ADMIN_SECRET=
ENV VITE_API_URL=$VITE_API_URL \
    VITE_WS_URL=$VITE_WS_URL \
    VITE_FEED_WS_URL=$VITE_FEED_WS_URL \
    VITE_NOTIFICATION_WS_URL=$VITE_NOTIFICATION_WS_URL \
    VITE_ADMIN_SECRET=$VITE_ADMIN_SECRET

RUN npm run build

# Stage 2: Serve the application with Nginx
FROM nginx:alpine AS final
COPY --from=build /app/dist /usr/share/nginx/html
# Add a custom nginx configuration to handle React Router (SPA)
RUN printf "server {\n    listen 80;\n    server_name localhost;\n    location / {\n        root /usr/share/nginx/html;\n        index index.html;\n        try_files \$uri \$uri/ /index.html;\n    }\n}\n" > /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
