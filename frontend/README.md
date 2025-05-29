# 🏴‍☠️ Treasure Hunt Solver - Frontend

React TypeScript frontend for the Treasure Hunt Solver application with Material-UI components and async operation support.

## Features

- **Interactive Matrix Input**: Dynamic grid with validation and visual feedback
- **Asynchronous Operations**: Support for long-running solve operations with real-time status updates
- **Cancellation Support**: Ability to cancel in-progress operations
- **Virtualized Solution Paths**: Optimized rendering for large solution paths
- **Paginated History**: Browse through previous treasure hunts with pagination
- **Responsive Design**: Works seamlessly on desktop and mobile devices
- **Material-UI**: Modern, accessible interface components

## Environment Setup

Create a `.env` file in the frontend directory:

```env
REACT_APP_API_BASE_URL=http://localhost:5001
```

See `ENVIRONMENT.md` for detailed configuration options.

## Architecture Overview

### Component Structure

```
src/
├── App.tsx                      # Main application with routing
├── types.ts                     # TypeScript interfaces
├── config.ts                    # API configuration
└── components/
    ├── MatrixInput.tsx          # Matrix grid input with validation
    ├── ParameterInput.tsx       # Problem parameter controls
    ├── ResultDisplay.tsx        # Solution results and status
    ├── VirtualizedSolutionPath.tsx # Performance-optimized path display
    └── History.tsx              # Paginated history browser
```

### Key TypeScript Interfaces

```typescript
// Treasure hunt problem definition
interface TreasureHuntRequest {
  n: number;
  m: number;
  p: number;
  matrix: number[][];
}

// Async operation tracking
interface AsyncSolveResponse {
  requestId: string;
  status: SolveStatus;
  message?: string;
}

// Solution path representation
interface PathStep {
  chestNumber: number;
  position: Position;
}

// Status enumeration
enum SolveStatus {
  Pending = 0,
  InProgress = 1,
  Completed = 2,
  Cancelled = 3,
  Failed = 4
}
```

### API Integration Patterns

The frontend integrates with the backend API using several patterns:

#### Synchronous Operations
For smaller problems (typically n×m×p < 1000), uses immediate solving:
```typescript
const response = await fetch('/api/treasure-hunt', {
  method: 'POST',
  body: JSON.stringify(request)
});
```

#### Asynchronous Operations
For larger problems, uses async workflow with polling:
```typescript
// Start async solve
const asyncResponse = await fetch('/api/treasure-hunt/solve-async', {
  method: 'POST',
  body: JSON.stringify(request)
});

// Poll for status
const checkStatus = async (requestId: string) => {
  const statusResponse = await fetch(`/api/treasure-hunt/solve-status/${requestId}`);
  // Handle status updates
};
```

#### Cancellation Support
```typescript
const cancelSolve = async (requestId: string) => {
  await fetch(`/api/treasure-hunt/cancel-solve/${requestId}`, {
    method: 'POST'
  });
};
```

### Performance Optimizations

#### VirtualizedSolutionPath Component
- Uses windowing for large solution paths (10,000+ steps)
- Only renders visible path steps
- Smooth scrolling with position indicators

#### Efficient State Management
- Debounced matrix input validation
- Optimistic UI updates for better responsiveness
- Smart polling intervals based on operation complexity

## Available Scripts

### `npm start`
Runs the frontend in development mode on [http://localhost:3000](http://localhost:3000)

### `npm test`
Launches the test runner with coverage for all components

### `npm run build`
Builds the optimized production bundle

### `npm run eject`
⚠️ **One-way operation** - Exposes webpack configuration

## Component Testing

Components include comprehensive test coverage:

```bash
# Run specific component tests
npm test -- MatrixInput.test.tsx
npm test -- History.test.tsx
npm test -- integration/
```

### Test Structure
- **Unit Tests**: Individual component behavior
- **Integration Tests**: Component interaction and API integration
- **Accessibility Tests**: ARIA compliance and keyboard navigation

## Development Guidelines

### State Management
- Use React hooks for local component state
- Lift state up for shared data (matrix, results)
- Use context for global settings (API base URL)

### Error Handling
- Display user-friendly error messages
- Graceful degradation for network issues
- Retry mechanisms for failed operations

### Accessibility
- ARIA labels for all interactive elements
- Keyboard navigation support
- Screen reader compatibility
- High contrast color support

## Deployment

The frontend is designed to be deployed as a static site:

```bash
npm run build
# Deploy the build/ folder to your hosting service
```

Compatible with:
- Netlify
- Vercel
- GitHub Pages
- AWS S3 + CloudFront
- Azure Static Web Apps

## Browser Support

- Chrome 90+
- Firefox 88+
- Safari 14+
- Edge 90+

## Related Documentation

- [Backend API Documentation](../README.md#api-endpoints)
- [Environment Configuration](./ENVIRONMENT.md)
- [Testing Guide](./src/components/__tests__/README.md)
