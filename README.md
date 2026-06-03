# Conway's Game of Life RESTful API

A production-ready REST API implementation of [Conway's Game of Life](https://en.wikipedia.org/wiki/Conway%27s_Game_of_Life) built with .NET 8.

## Overview

This API allows you to:
- Upload and store board states
- Compute next generation states
- Calculate N generations ahead
- Find final stable states (static or oscillating patterns)

The service persists board states using a configurable database (in-memory by default, PostgreSQL for production).

## Tech Stack

- **.NET 8.0** - Modern cross-platform framework
- **ASP.NET Core** - Web API framework
- **Entity Framework Core** - ORM for database access
- **Database** - Configurable: InMemory (default) or PostgreSQL
- **FluentValidation** - Input validation
- **xUnit** - Testing framework
- **Moq** - Mocking framework for unit tests

> **Note:** The API defaults to an **in-memory database** for easy setup with no dependencies. To use PostgreSQL for production persistence, change `"DatabaseProvider": "PostgreSQL"` in `appsettings.json` and follow the database setup steps below.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- (Optional for PostgreSQL) [PostgreSQL 12+](https://www.postgresql.org/download/)
- (Optional) [pgAdmin](https://www.pgadmin.org/) for database management

## Quick Start

### 1. Clone and Navigate

```bash
cd ~/code/demos/conways-game-life
```

### 2. Run the Application

```bash
cd ConwaysGameOfLifeApi
dotnet run
```

The API will start (default: `http://localhost:5282`) using an in-memory database.

### 3. Test the API

Use curl, Postman, or any HTTP client to test the endpoints. See [EXAMPLES.md](EXAMPLES.md) for sample requests.

---

### Optional: Using PostgreSQL

To use PostgreSQL instead of the in-memory database:

**1. Update configuration** in `ConwaysGameOfLifeApi/appsettings.json`:

```json
{
  "DatabaseProvider": "PostgreSQL",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=conways_game_of_life;Username=postgres;Password=postgres"
  }
}
```

**2. Create database:**

```sql
CREATE DATABASE conways_game_of_life;
```

**3. Run migrations:**

```bash
cd ConwaysGameOfLifeApi
dotnet ef database update
```

**4. Run the application:**

```bash
dotnet run
```

## Running Tests

Run all tests (unit + integration):

```bash
dotnet test
```

Run only unit tests:

```bash
dotnet test --filter "FullyQualifiedName~Unit"
```

Run only integration tests:

```bash
dotnet test --filter "FullyQualifiedName~Integration"
```

## API Endpoints

### 1. Upload Board State

**POST** `/api/boards`

Upload a new board configuration.

**Request Body:**
```json
{
  "boardData": [
    [true, false, true],
    [false, true, false],
    [true, false, true]
  ]
}
```

**Response:** `201 Created`
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

### 2. Get Next Generation

**GET** `/api/boards/{id}/next`

Returns the next generation state of the board.

**Response:** `200 OK`
```json
{
  "boardData": [
    [false, true, false],
    [true, false, true],
    [false, true, false]
  ],
  "generation": 1
}
```

### 3. Get N Generations Ahead

**GET** `/api/boards/{id}/generations/{n}`

Returns the board state after N generations.

**Parameters:**
- `n` - Number of generations (positive integer, max 1,000,000)

**Response:** `200 OK`
```json
{
  "boardData": [[false, false, false], [true, true, true], [false, false, false]],
  "generation": 100
}
```

### 4. Get Final State

**GET** `/api/boards/{id}/final`

Returns the final stable state of the board.

**Response:** `200 OK` (Static pattern)
```json
{
  "boardData": [[true, true], [true, true]],
  "generationCount": 0,
  "stateType": "static",
  "period": null
}
```

**Response:** `200 OK` (Oscillating pattern)
```json
{
  "boardData": [[false, true, false], [false, true, false], [false, true, false]],
  "generationCount": 2,
  "stateType": "oscillating",
  "period": 2
}
```

**Response:** `422 Unprocessable Entity` (Timeout)
```json
{
  "message": "Board did not reach a stable state within 10000 iterations..."
}
```

### Error Responses

- **400 Bad Request** - Invalid input (e.g., empty board, ragged array)
- **404 Not Found** - Board ID doesn't exist
- **422 Unprocessable Entity** - Final state timeout (non-stabilizing board)
- **500 Internal Server Error** - Unexpected server error

## Conway's Game of Life Rules

The simulation follows these rules for each generation:

1. **Underpopulation:** A live cell with < 2 live neighbors dies
2. **Survival:** A live cell with 2-3 live neighbors survives
3. **Overpopulation:** A live cell with > 3 live neighbors dies
4. **Reproduction:** A dead cell with exactly 3 live neighbors becomes alive

## Configuration

Configuration is managed in `appsettings.json`:

```json
{
  "GameSettings": {
    "MaxBoardWidth": 1000,
    "MaxBoardHeight": 1000,
    "MaxIterationsForFinalState": 10000,
    "MaxGenerationsAhead": 1000000,
    "CycleDetectionHistorySize": 1000
  }
}
```

- **MaxBoardWidth/Height** - Maximum board dimensions (prevents abuse)
- **MaxIterationsForFinalState** - Timeout for finding stable states
- **MaxGenerationsAhead** - Maximum N value for N-ahead queries
- **CycleDetectionHistorySize** - How many states to track for cycle detection

## Architecture

### Project Structure

```
ConwaysGameOfLifeApi/
├── Controllers/          # HTTP endpoints (API layer)
├── Services/            # Business logic
├── Repositories/        # Data access layer
├── GameEngine/          # Conway's rules implementation
├── Models/
│   ├── Domain/          # Database entities
│   └── DTOs/            # API request/response models
├── Validators/          # Input validation
├── Middleware/          # Error handling
├── Configuration/       # App settings models
└── Data/                # DbContext
```

### Design Patterns

- **Repository Pattern** - Abstracts database operations
- **Service Layer Pattern** - Encapsulates business logic
- **Dependency Injection** - Built-in .NET DI container
- **Options Pattern** - Type-safe configuration
- **Middleware Pipeline** - Global error handling

### Key Design Decisions

1. **Compute On-Demand** - Generations are computed when requested, not pre-cached. Simpler implementation, sufficient for exercise scope.

2. **Cycle Detection** - Uses SHA256 hashing of board states for efficient comparison. Detects both static patterns (period 1) and oscillators (period > 1).

3. **JSONB Storage** - Board states stored as JSONB in PostgreSQL for efficient storage and potential querying.

4. **Finite Grid** - Out-of-bounds cells treated as dead (standard interpretation), not toroidal wrap-around.

5. **In-Memory Testing** - Integration tests use EF Core in-memory provider for fast, isolated testing.

## Performance Considerations

- **Large Boards** - Maximum 1000×1000 boards (configurable). Larger boards will be rejected.
- **High N Values** - Computing 1M generations may take several seconds. Consider adding request timeout warnings.
- **Final State Timeouts** - Boards that don't stabilize within 10K iterations return 422 error. Glider guns and other growing patterns won't have a "final state".
- **Database I/O** - Each request loads board from database. For high-frequency access, consider caching.

## Known Patterns

### Static (Still Life)
- **Block** - 2×2 square of live cells
- **Beehive** - 6-cell stable pattern

### Oscillators
- **Blinker** - Period 2, alternates between horizontal and vertical
- **Toad** - Period 2, 6-cell oscillator
- **Pulsar** - Period 3, 13×13 pattern

### Moving Patterns
- **Glider** - Moves diagonally every 4 generations
- **Lightweight Spaceship (LWSS)** - Moves horizontally
