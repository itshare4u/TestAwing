# Environment Configuration

This project uses environment variables to configure the API URL and other settings.

## Setup

1. Copy the example environment file:
   ```bash
   cp .env.example .env
   ```

2. Update the `.env` file with your specific configuration:
   ```bash
   # API Configuration
   REACT_APP_API_URL=http://localhost:5001/api
   REACT_APP_ENVIRONMENT=development
   ```

## Environment Files

- **`.env`** - Your local environment variables (gitignored)
- **`.env.example`** - Template file with example values
- **`.env.development`** - Development environment defaults
- **`.env.production`** - Production environment configuration

## Environment Variables

- `REACT_APP_API_URL` - Backend API base URL
- `REACT_APP_ENVIRONMENT` - Current environment (development/production)
- `REACT_APP_DEBUG` - Enable debug logging (true/false)

## Usage

The configuration is automatically loaded from `src/config.ts`:

```typescript
import config from './config';

// Use the API URL
const response = await axios.get(`${config.apiUrl}/endpoint`);

// Check environment
if (config.isDevelopment) {
  console.log('Running in development mode');
}
```

## Different Environments

### Development
```bash
REACT_APP_API_URL=http://localhost:5001/api
```

### Production
```bash
REACT_APP_API_URL=https://your-production-domain.com/api
```

### Custom Backend
```bash
REACT_APP_API_URL=http://your-custom-server:8080/api
```
