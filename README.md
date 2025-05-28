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
- ✅ Real-time solution calculation
- ✅ Database storage of all solutions
- ✅ History of previous treasure hunts
- ✅ Example problems pre-loaded
- ✅ **Random test data generation** (follows problem constraints)
- ✅ Material-UI modern interface
- ✅ Responsive design

## Problem Constraints

For a valid treasure hunt problem:
- **Each chest number from 1 to p must appear exactly once** in the matrix
- **p must equal n×m** (total number of positions)
- This ensures every chest has a unique key and the problem is solvable
- The random data generator automatically enforces these constraints

## Tech Stack

### Backend (C#)

- ASP.NET Core 9.0
- Entity Framework Core (In-Memory Database)
- RESTful API

### Frontend (React)

- React 18 with TypeScript
- Material-UI (MUI)
- Axios for API calls

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

## API Endpoints

### POST /api/treasure-hunt

Solve a treasure hunt problem

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

### GET /api/treasure-hunts

Get all previously solved treasure hunts

### GET /api/generate-random-data

Generate random test data that follows treasure hunt constraints

**Parameters:**
- `n` (optional): Number of rows (default: 3)
- `m` (optional): Number of columns (default: 3)  
- `p` (optional): Will be automatically set to n×m for valid treasure hunt

**Example:** `/api/generate-random-data?n=4&m=4`

Returns a valid treasure hunt matrix where each number from 1 to p appears exactly once.

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
├── TestAwing/                 # C# Backend
│   ├── Models/               # Data models
│   ├── Services/             # Business logic
│   └── Program.cs           # API configuration
├── frontend/                 # React Frontend
│   └── src/
│       ├── App.tsx          # Main component
│       └── index.tsx        # Entry point
├── package.json             # Root package configuration
└── README.md               # This file
```
