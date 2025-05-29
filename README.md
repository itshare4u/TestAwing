# 🏴‍☠️ Treasure Hunt Solver

A full-stack application that solves the pirate treasure hunt problem using C# backend with Entity Framework and React
frontend with Material-UI.

## Problem Description

Pirates have found a treasure map with an n×m matrix of islands. Each island contains a chest numbered from 1 to p,
where each chest contains the key for the next numbered chest. The treasure is in chest p. Starting from position (1,1)
with key 0, find the minimum fuel needed to reach the treasure.

Fuel required to travel from (x1,y1) to (x2,y2) = √((x1-x2)² + (y1-y2)²)

## Features

- ✅ Interactive matrix input with validation
- ✅ **Synchronous and asynchronous solution calculation**
- ✅ **Real-time status checking for long-running operations**
- ✅ **Cancellation support for async operations**
- ✅ Database storage of all solutions with detailed paths
- ✅ **Paginated history of previous treasure hunts**
- ✅ Example problems pre-loaded
- ✅ **Random test data generation** (follows problem constraints)
- ✅ Material-UI modern interface with virtualized solution paths
- ✅ Responsive design
- ✅ **🐳 Docker containerization** with production-ready configuration

## 🚀 Quick Start with Docker

### Production Setup with SQL Server

For production deployment with SQL Server database:

```bash
# Clone and navigate to the project
git clone <repository-url>
cd TestAwing

# Start all services including SQL Server
docker-compose up -d

# Check services status
docker-compose ps

# Test the application
curl http://localhost:5001/health
curl "http://localhost:5001/api/generate-random-data?n=3&m=3&p=3"
```

The full production application will be running at:

- **Frontend**: http://localhost:3001 ✨ (React app with nginx)
- **Backend API**: http://localhost:5001 (ASP.NET Core API with SQL Server)
- **SQL Server**: localhost:1433 (Database server)

### Development Setup (In-Memory Database)

For quick development/testing without SQL Server:

```bash
# Use the test configuration (in-memory database)
docker-compose -f compose.test.yaml up -d
```

Want to try the full application immediately? Run these commands:

```bash
# Clone and navigate to the project
git clone <repository-url>
cd TestAwing

# Start both frontend and backend services
docker-compose up -d

# Test the application
curl http://localhost:5001/health
curl "http://localhost:5001/api/generate-random-data?n=3&m=3&p=3"
```

The full application will be running at:
- **Frontend**: http://localhost:3001 ✨ (React app with nginx)
- **Backend API**: http://localhost:5001 (ASP.NET Core API)

## Problem Constraints

For a valid treasure hunt problem:
- **Each chest number from 1 to p must appear at least once** in the matrix
- **Matrix size (n×m) must be ≥ p** to accommodate all chest numbers
- **Multiple chests can have the same number** (multiple keys for same chest)
- The random data generator ensures these constraints are met

## Tech Stack

### Backend (C#)

- ASP.NET Core 9.0
- Entity Framework Core with multiple database providers:
    - **In-Memory Database** (development/testing)
    - **SQL Server** (production)
    - **SQLite** (lightweight option)
    - **MySQL** (alternative production option)
- RESTful API
- EF Core Migrations for database schema management

### Frontend (React)

- React 18 with TypeScript
- Material-UI (MUI)
- Axios for API calls

### DevOps & Deployment

- 🐳 **Docker** with multi-stage builds for both frontend and backend
- **Docker Compose** for microservices orchestration
- **Nginx** reverse proxy for production-grade frontend serving
- **Health checks** for container monitoring
- **Non-root security** configuration
- **API proxy** for seamless frontend-backend communication

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- Node.js (v16 or higher)
- npm

### Installation

1. Clone the repository
2. Install dependencies:

```bash
npm run restore
```

### Running the Application

#### Option 1: Run both backend and frontend together

```bash
npm run dev
```

#### Option 2: Run separately

```bash
# Terminal 1 - Backend
npm run backend

# Terminal 2 - Frontend  
npm run frontend
```

The application will be available at:

- **Frontend**: http://localhost:3001 (Full React application)
- **Backend API**: http://localhost:5001 (Direct API access)

## 🐳 Docker Deployment

The application uses a **microservices architecture** with separate Docker containers for frontend and backend.

### Prerequisites for Docker

- Docker Desktop installed and running
- Docker Compose (included with Docker Desktop)

### Architecture Overview

**Production Architecture (with SQL Server):**
```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Frontend      │    │   Backend       │    │   SQL Server    │
│   (React/nginx) │◄──►│   (ASP.NET)     │◄──►│   Database      │
│   Port: 3001    │    │   Port: 5001    │    │   Port: 1433    │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

**Development Architecture (In-Memory Database):**

```
┌─────────────────┐    ┌─────────────────┐
│   Frontend      │    │   Backend       │
│   (React/nginx) │◄──►│   (ASP.NET)     │
│   Port: 3001    │    │   Port: 5001    │
└─────────────────┘    └─────────────────┘
```

- **Frontend**: React application served by nginx with API proxy
- **Backend**: ASP.NET Core API with Entity Framework
- **Database**: SQL Server (production) or In-Memory (development)
- **Communication**: Frontend calls backend via `/api/*` endpoints

### Quick Start with Docker Compose

```bash
# Start both services
docker-compose up -d

# View running containers
docker-compose ps

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

**Access Points:**
- **Full Application**: http://localhost:3001 (nginx serves React + proxies API)
- **Direct Backend API**: http://localhost:5001 (for testing/development)

### Individual Service Management

#### Backend Only

```bash
cd TestAwing
docker build -t testawing-backend:latest .
docker run -d --name testawing-backend -p 5001:8080 testawing-backend:latest
```

#### Frontend Only

```bash
cd frontend
docker build -t testawing-frontend:latest .
docker run -d --name testawing-frontend -p 3001:80 testawing-frontend:latest
```

### Docker Compose Configuration

The `compose.yaml` defines two services:

```yaml
services:
  backend:
    build: ./TestAwing
    ports:
      - "5001:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8080
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  frontend:
    build: ./frontend
    ports:
      - "3001:80"
    environment:
      - REACT_APP_API_URL=http://localhost:5001/api
    depends_on:
      - backend
```

### Docker Features

#### Backend Container
- **Multi-stage build** for optimized image size
- **Security hardening** with non-root user execution
- **Health checks** for container monitoring
- **Environment variables** for configuration
- **Production-ready** ASP.NET Core configuration
- **SQL Server tools** integrated for production database operations
- **EF Core migrations** automatically applied on startup

#### Frontend Container
- **Nginx** web server for production-grade static file serving
- **API proxy** configuration for seamless backend communication
- **Environment variable** injection at build time
- **Optimized React build** with code splitting and minification

### Container Management

```bash
# View all container logs
docker-compose logs

# View specific service logs
docker-compose logs backend
docker-compose logs frontend

# Scale services (if needed)
docker-compose up -d --scale backend=2

# Restart specific service
docker-compose restart backend

# Access container shell (for debugging)
docker exec -it testawing-backend-1 /bin/bash
docker exec -it testawing-frontend-1 /bin/sh

# Health check endpoints
curl http://localhost:5001/health
curl http://localhost:3001/api/health  # via nginx proxy
```

### Docker Environment Variables

#### Backend Container
- `ASPNETCORE_URLS=http://+:8080` - Application binding URL
- `ASPNETCORE_ENVIRONMENT=Development` - Runtime environment
- `DatabaseProvider=InMemory/SqlServer/SQLite/MySQL` - Database provider selection
- `ConnectionStrings__DefaultConnection=...` - Database connection string (for SQL Server/SQLite/MySQL)
- `TZ=UTC` - Timezone configuration

#### Frontend Container
- `REACT_APP_API_URL=http://localhost:5001/api` - Backend API endpoint
- `REACT_APP_ENVIRONMENT=development` - Application environment
- `REACT_APP_DEBUG=true` - Debug mode flag

### Docker Images Information

#### Backend Image (`testawing-backend`)
- **Base Image**: `mcr.microsoft.com/dotnet/aspnet:9.0`
- **Build Image**: `mcr.microsoft.com/dotnet/sdk:9.0`
- **Final Size**: ~576MB
- **Security**: Runs as non-root user (`appuser`)
- **Health Check**: Automatic monitoring via `/health` endpoint
- **Database Support**: SQL Server tools (mssql-tools18) for production database connectivity
- **Migrations**: EF Core migrations automatically applied during container startup

#### Frontend Image (`testawing-frontend`)
- **Base Images**: `node:18-alpine` (build), `nginx:alpine` (runtime)
- **Final Size**: ~25MB
- **Security**: Runs as nginx user
- **Features**: Optimized static file serving + API proxy

### Production Deployment Considerations

When deploying to production:

1. **Update API URL**: Modify `REACT_APP_API_URL` in frontend Dockerfile
2. **SSL/TLS**: Configure nginx with SSL certificates
3. **Environment**: Set `ASPNETCORE_ENVIRONMENT=Production`
4. **Database**: Configure SQL Server connection string
5. **Monitoring**: Set up health check monitoring
6. **Scaling**: Use Docker Swarm or Kubernetes for scaling
7. **Security**: Update default SQL Server password
8. **Backup**: Configure database backup strategy

Example production environment variables:
```bash
# Backend
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=https://+:443;http://+:80
DatabaseProvider=SqlServer
ConnectionStrings__DefaultConnection="Server=your-sql-server;Database=TreasureHuntDb;User=your-user;Password=your-secure-password;TrustServerCertificate=true;"

# SQL Server
SA_PASSWORD=YourSecurePassword123!
MSSQL_PID=Standard

# Frontend
REACT_APP_API_URL=https://api.yourdomain.com/api
REACT_APP_ENVIRONMENT=production
REACT_APP_DEBUG=false
```

## API Endpoints

### Synchronous Solving

#### POST /api/treasure-hunt

Solve a treasure hunt problem immediately (for smaller problems)

**Request:**
```json
{
  "n": 3,
  "m": 3, 
  "p": 3,
  "matrix": [
    [3, 2, 2],
    [2, 2, 2], 
    [2, 2, 1]
  ]
}
```

**Response:**
```json
{
  "id": 1,
  "n": 3,
  "m": 3,
  "p": 3,
  "matrix": [[3, 2, 2], [2, 2, 2], [2, 2, 1]],
  "minFuel": 5.65685,
  "path": [
    {"chestNumber": 0, "position": {"row": 1, "col": 1}},
    {"chestNumber": 1, "position": {"row": 3, "col": 3}},
    {"chestNumber": 2, "position": {"row": 1, "col": 2}},
    {"chestNumber": 3, "position": {"row": 1, "col": 1}}
  ],
  "solvedAt": "2025-05-29T10:30:00Z"
}
```

### Asynchronous Solving

#### POST /api/treasure-hunt/solve-async

Start an asynchronous solve operation (recommended for larger problems)

**Request:** Same as synchronous endpoint

**Response:**
```json
{
  "requestId": 1,
  "status": "Pending",
  "message": "Solve request has been queued"
}
```

#### GET /api/treasure-hunt/solve-status/{id}

Check the status of an asynchronous solve operation

**Response:**
```json
{
  "requestId": 1,
  "status": "Completed",
  "result": {
    "id": 1,
    "n": 3,
    "m": 3,
    "p": 3,
    "matrix": [[3, 2, 2], [2, 2, 2], [2, 2, 1]],
    "minFuel": 5.65685,
    "path": [...],
    "solvedAt": "2025-05-29T10:30:00Z"
  }
}
```

**Status Values:**
- `Pending` (0): Request queued but not started
- `InProgress` (1): Currently being solved
- `Completed` (2): Successfully solved
- `Cancelled` (3): Cancelled by user
- `Failed` (4): Failed due to error

#### POST /api/treasure-hunt/cancel-solve/{id}

Cancel a pending or in-progress asynchronous solve operation

**Response:**
```json
{
  "requestId": 1,
  "status": "Cancelled",
  "message": "Solve request has been cancelled"
}
```

### History and Data Management

#### GET /api/treasure-hunts

Get paginated list of previously solved treasure hunts

**Parameters:**
- `page` (optional): Page number (default: 1)
- `pageSize` (optional): Items per page (default: 8, max: 50)

**Response:**
```json
{
  "items": [
    {
      "id": 1,
      "n": 3,
      "m": 3,
      "p": 3,
      "minFuel": 5.65685,
      "solvedAt": "2025-05-29T10:30:00Z"
    }
  ],
  "totalCount": 15,
  "page": 1,
  "pageSize": 8,
  "totalPages": 2
}
```

#### GET /api/treasure-hunt/{id}

Get detailed information about a specific treasure hunt including the solution path

**Response:**
```json
{
  "id": 1,
  "n": 3,
  "m": 3,
  "p": 3,
  "matrix": [[3, 2, 2], [2, 2, 2], [2, 2, 1]],
  "minFuel": 5.65685,
  "path": [
    {"chestNumber": 0, "position": {"row": 1, "col": 1}},
    {"chestNumber": 1, "position": {"row": 3, "col": 3}},
    {"chestNumber": 2, "position": {"row": 1, "col": 2}},
    {"chestNumber": 3, "position": {"row": 1, "col": 1}}
  ],
  "solvedAt": "2025-05-29T10:30:00Z"
}
```

Returns a valid treasure hunt matrix where numbers 1-6 each appear at least once.

## Algorithm

The solution uses **Dynamic Programming** approach for optimal pathfinding:

### Core Algorithm

1. **State Definition**: `dp[chest][row][col]` = minimum fuel to reach position `(row, col)` after collecting chest
   number `chest`

2. **Base Case**: Start at position `(1,1)` with key 0
    - `dp[0][1][1] = 0` (no fuel needed to start)

3. **State Transition**: For each chest from 1 to p:
    - Find all positions containing the current chest number
    - For each valid previous state, calculate the fuel needed to move to each chest position
    - Update the minimum fuel for reaching each chest position
    - `dp[chest][newRow][newCol] = min(dp[chest-1][oldRow][oldCol] + distance)`

4. **Final Result**: The minimum value among all `dp[p][row][col]` positions

### Distance Calculation

Fuel required to travel from `(x1,y1)` to `(x2,y2)` = `√((x1-x2)² + (y1-y2)²)`

### Key Optimizations

- **Memoization**: Stores computed states to avoid recalculation
- **Pruning**: Only explores reachable states to reduce complexity
- **Early Termination**: Stops exploring paths that exceed current minimum
- **Asynchronous Processing**: Large problems solved in background with cancellation support

### Time Complexity

- **Time**: O(p × n × m × k) where k is the average number of positions per chest
- **Space**: O(p × n × m) for the DP table
- **Practical**: Optimized for real-world constraints with sparse chest distributions

## Project Structure

```
TestAwing/
├── TestAwing/                    # C# Backend
│   ├── Models/
│   │   ├── TreasureHuntModels.cs # API models and data structures
│   │   └── TreasureHuntContext.cs # Entity Framework context
│   ├── Services/
│   │   ├── TreasureHuntSolverService.cs # Async solving logic
│   │   └── TreasureHuntDataService.cs   # Data persistence
│   ├── Dockerfile               # 🐳 Backend Docker configuration
│   └── Program.cs                # API configuration and endpoints
├── frontend/                     # React Frontend
│   ├── Dockerfile               # 🐳 Frontend Docker configuration
│   ├── nginx.conf               # 🐳 Nginx proxy configuration
│   └── src/
│       ├── App.tsx              # Main application component
│       ├── types.ts             # TypeScript type definitions
│       ├── config.ts            # API configuration
│       └── components/
│           ├── MatrixInput.tsx          # Matrix input interface
│           ├── ParameterInput.tsx       # Problem parameters
│           ├── ResultDisplay.tsx        # Solution results
│           ├── VirtualizedSolutionPath.tsx # Optimized path display
│           └── History.tsx              # Paginated history view
├── TestAwing.Tests/             # Unit and integration tests
├── package.json                 # Root package configuration
├── compose.yaml                 # 🐳 Docker microservices composition
└── README.md                   # This file
```

