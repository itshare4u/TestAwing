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

## 🚀 Quick Docker Demo

Want to try the application immediately? Run these commands:

```bash
# Clone and navigate to the project
git clone <repository-url>
cd TestAwing

# Build and run with Docker
cd TestAwing
docker build -t testawing:latest .
docker run -d --name testawing-container -p 5001:8080 testawing:latest

# Test the API
curl http://localhost:5001/health
curl "http://localhost:5001/api/generate-random-data?n=3&m=3&p=3"
```

The backend API will be running at http://localhost:5001 ✨

## Problem Constraints

For a valid treasure hunt problem:
- **Each chest number from 1 to p must appear at least once** in the matrix
- **Matrix size (n×m) must be ≥ p** to accommodate all chest numbers
- **Multiple chests can have the same number** (multiple keys for same chest)
- The random data generator ensures these constraints are met

## Tech Stack

### Backend (C#)

- ASP.NET Core 9.0
- Entity Framework Core (In-Memory Database)
- RESTful API

### Frontend (React)

- React 18 with TypeScript
- Material-UI (MUI)
- Axios for API calls

### DevOps & Deployment

- 🐳 **Docker** with multi-stage builds
- **Docker Compose** for full-stack deployment
- **Health checks** for container monitoring
- **Non-root security** configuration

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

- Frontend: http://localhost:3000
- Backend API: http://localhost:5001

## 🐳 Docker Deployment

### Prerequisites for Docker

- Docker Desktop installed and running
- Docker Compose (included with Docker Desktop)

### Quick Start with Docker

#### Option 1: Run Backend Only with Docker

```bash
# Build the Docker image
cd TestAwing
docker build -t testawing:latest .

# Run the container
docker run -d --name testawing-container -p 5001:8080 testawing:latest

# Check container status
docker ps
```

The backend API will be available at: http://localhost:5001

#### Option 2: Full Stack with Docker Compose

```bash
# Run both backend and frontend
docker-compose up -d

# Stop the services
docker-compose down
```

### Docker Features

- **Multi-stage build** for optimized image size (576MB)
- **Security hardening** with non-root user execution
- **Health checks** for container monitoring
- **Environment variables** for configuration
- **Production-ready** configuration

### Docker Container Management

```bash
# View container logs
docker logs testawing-container

# View container details
docker inspect testawing-container

# Access container shell (for debugging)
docker exec -it testawing-container /bin/bash

# Health check endpoint
curl http://localhost:5001/health
```

### Docker Environment Variables

The container supports the following environment variables:

- `ASPNETCORE_URLS=http://+:8080` - Application binding URL
- `ASPNETCORE_ENVIRONMENT=Production` - Runtime environment
- `DatabaseProvider=InMemory` - Database provider (InMemory/SQLite/MySQL/SqlServer)
- `TZ=UTC` - Timezone configuration

### Docker Image Information

- **Base Image**: `mcr.microsoft.com/dotnet/aspnet:9.0`
- **Build Image**: `mcr.microsoft.com/dotnet/sdk:9.0`
- **Final Size**: ~576MB
- **Security**: Runs as non-root user (`appuser`)
- **Health Check**: Automatic monitoring via `/health` endpoint

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
  "id": "550e8400-e29b-41d4-a716-446655440000",
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
  "requestId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Pending",
  "message": "Solve request has been queued"
}
```

#### GET /api/treasure-hunt/solve-status/{id}

Check the status of an asynchronous solve operation

**Response:**
```json
{
  "requestId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Completed",
  "result": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
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
  "requestId": "550e8400-e29b-41d4-a716-446655440000",
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
      "id": "550e8400-e29b-41d4-a716-446655440000",
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
  "id": "550e8400-e29b-41d4-a716-446655440000",
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

### Utility Endpoints

#### GET /api/generate-random-data

Generate random test data that follows treasure hunt constraints

**Parameters:**
- `n` (optional): Number of rows (default: 3)
- `m` (optional): Number of columns (default: 3)  
- `p` (optional): Maximum chest number (default: min(n×m, 10))

**Constraints:**
- n×m must be ≥ p (matrix must have enough positions for all chest numbers)
- Each number from 1 to p will appear at least once
- Remaining positions filled with random numbers from 1 to p

**Example:** `/api/generate-random-data?n=4&m=4&p=6`

**Response:**
```json
{
  "n": 4,
  "m": 4,
  "p": 6,
  "matrix": [
    [1, 2, 3, 4],
    [5, 6, 1, 2],
    [3, 4, 5, 6],
    [1, 2, 3, 4]
  ]
}
```

Returns a valid treasure hunt matrix where numbers 1-6 each appear at least once.

### Error Handling

All endpoints return appropriate HTTP status codes:
- `200 OK`: Successful operation
- `400 Bad Request`: Invalid input parameters
- `404 Not Found`: Resource not found
- `409 Conflict`: Operation conflict (e.g., trying to cancel completed solve)
- `500 Internal Server Error`: Server error

Error responses include descriptive messages:
```json
{
  "error": "Matrix validation failed",
  "details": "Each chest number from 1 to p must appear at least once in the matrix"
}
```

## Example Test Cases

### Test 1

- **Input**: n=3, m=3, p=3
- **Matrix**:
  ```
  3 2 2
  2 2 2
  2 2 1
  ```
- **Expected Output**: 4√2 ≈ 5.65685

### Test 2

- **Input**: n=3, m=4, p=3
- **Matrix**:
  ```
  2 1 1 1
  1 1 1 1
  2 1 1 3
  ```
- **Expected Output**: 5

### Test 3

- **Input**: n=3, m=4, p=12
- **Matrix**:
  ```
  1  2  3  4
  8  7  6  5
  9  10 11 12
  ```
- **Expected Output**: 11

## Algorithm

The solution uses a greedy approach:

1. Start at position (1,1) with key 0
2. For each chest from 1 to p:
    - Find the chest's position in the matrix
    - Calculate Euclidean distance from current position
    - Add distance to total fuel
    - Move to chest position
3. Return total fuel required

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
│   ├── Dockerfile               # 🐳 Docker configuration
│   └── Program.cs                # API configuration and endpoints
├── frontend/                     # React Frontend
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
├── compose.yaml                 # 🐳 Docker composition
└── README.md                   # This file
```
