// Environment configuration utility
export const config = {
  apiUrl: process.env.REACT_APP_API_URL || 'http://localhost:5001/api',
  environment: process.env.REACT_APP_ENVIRONMENT || 'development',
  isDebug: process.env.REACT_APP_DEBUG === 'true',
  isDevelopment: process.env.NODE_ENV === 'development',
  isProduction: process.env.NODE_ENV === 'production',
} as const;

// Helper function to log configuration in development
if (config.isDevelopment) {
  console.log('🔧 App Configuration:', {
    apiUrl: config.apiUrl,
    environment: config.environment,
    debug: config.isDebug,
  });
}

export default config;
