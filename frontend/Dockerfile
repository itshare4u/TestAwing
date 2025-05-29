# Frontend Dockerfile for React app
FROM node:18-alpine AS build

# Set working directory
WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm ci --only=production

# Copy source code
COPY . .

# Set environment variables for build
ENV REACT_APP_API_URL=http://localhost:5001/api
ENV REACT_APP_ENVIRONMENT=development
ENV REACT_APP_DEBUG=true

# Build the React app
RUN npm run build

# Production stage - use nginx to serve static files
FROM nginx:alpine

# Copy built React app to nginx
COPY --from=build /app/build /usr/share/nginx/html

# Copy nginx configuration
COPY nginx.conf /etc/nginx/nginx.conf

# Expose port 3000
EXPOSE 3000

# Start nginx
CMD ["nginx", "-g", "daemon off;"]
