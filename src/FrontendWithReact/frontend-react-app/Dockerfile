# Stage 1: Build the React app
FROM node:20 AS build

WORKDIR /app

COPY package.json package-lock.json ./

RUN npm install

COPY . .

RUN npm run build

# Stage 2: Serve the React app with Nginx
FROM nginx:alpine

# Copy the built files from the previous stage
COPY --from=build /app/dist /usr/share/nginx/html

# Copy the Nginx configuration template
COPY --from=build /app/default.conf.template /etc/nginx/templates/default.conf.template

# Expose the default Nginx port
EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]