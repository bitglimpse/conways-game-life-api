# API Examples

Quick reference for testing the Conway's Game of Life API.

## Setup

Start the API:
```bash
cd ConwaysGameOfLifeApi
dotnet run
```

API will be available at: `https://localhost:5001`

## Example Requests (using curl)

### 1. Upload a Blinker Pattern

```bash
curl -X POST https://localhost:5001/api/boards \
  -H "Content-Type: application/json" \
  -d '{
    "boardData": [
      [false, false, false, false, false],
      [false, false, false, false, false],
      [false, true, true, true, false],
      [false, false, false, false, false],
      [false, false, false, false, false]
    ]
  }'
```

Response:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

### 2. Get Next Generation

```bash
curl https://localhost:5001/api/boards/3fa85f64-5717-4562-b3fc-2c963f66afa6/next
```

Response:
```json
{
  "boardData": [
    [false, false, false, false, false],
    [false, false, true, false, false],
    [false, false, true, false, false],
    [false, false, true, false, false],
    [false, false, false, false, false]
  ],
  "generation": 1
}
```

### 3. Get 10 Generations Ahead

```bash
curl https://localhost:5001/api/boards/3fa85f64-5717-4562-b3fc-2c963f66afa6/generations/10
```

### 4. Get Final State

```bash
curl https://localhost:5001/api/boards/3fa85f64-5717-4562-b3fc-2c963f66afa6/final
```

Response (for oscillating pattern):
```json
{
  "boardData": [
    [false, false, false, false, false],
    [false, false, true, false, false],
    [false, false, true, false, false],
    [false, false, true, false, false],
    [false, false, false, false, false]
  ],
  "generationCount": 2,
  "stateType": "oscillating",
  "period": 2
}
```

## Classic Patterns

### Glider
```json
{
  "boardData": [
    [false, true, false, false, false, false],
    [false, false, true, false, false, false],
    [true, true, true, false, false, false],
    [false, false, false, false, false, false],
    [false, false, false, false, false, false],
    [false, false, false, false, false, false]
  ]
}
```

### Block (Static)
```json
{
  "boardData": [
    [false, false, false, false],
    [false, true, true, false],
    [false, true, true, false],
    [false, false, false, false]
  ]
}
```

### Toad (Period-2 Oscillator)
```json
{
  "boardData": [
    [false, false, false, false, false, false],
    [false, false, false, false, false, false],
    [false, false, true, true, true, false],
    [false, true, true, true, false, false],
    [false, false, false, false, false, false],
    [false, false, false, false, false, false]
  ]
}
```

### Beacon (Period-2 Oscillator)
```json
{
  "boardData": [
    [false, false, false, false, false, false],
    [false, true, true, false, false, false],
    [false, true, true, false, false, false],
    [false, false, false, true, true, false],
    [false, false, false, true, true, false],
    [false, false, false, false, false, false]
  ]
}
```

## Using PowerShell (Windows)

```powershell
# Upload board
$body = @{
    boardData = @(
        @($false, $true, $false),
        @($false, $true, $false),
        @($false, $true, $false)
    )
} | ConvertTo-Json -Depth 3

Invoke-RestMethod -Uri "https://localhost:5001/api/boards" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body

# Get next state
Invoke-RestMethod -Uri "https://localhost:5001/api/boards/{id}/next"
```

## Using JavaScript (fetch)

```javascript
// Upload board
const response = await fetch('https://localhost:5001/api/boards', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    boardData: [
      [false, true, false],
      [false, true, false],
      [false, true, false]
    ]
  })
});

const { id } = await response.json();

// Get next state
const nextState = await fetch(`https://localhost:5001/api/boards/${id}/next`);
const result = await nextState.json();
console.log(result);
```

## Error Examples

### 400 Bad Request - Invalid Board

```bash
curl -X POST https://localhost:5001/api/boards \
  -H "Content-Type: application/json" \
  -d '{
    "boardData": []
  }'
```

Response:
```json
{
  "message": "Validation failed",
  "errors": {
    "BoardData": ["Board must have at least one row"]
  }
}
```

### 404 Not Found - Board Doesn't Exist

```bash
curl https://localhost:5001/api/boards/00000000-0000-0000-0000-000000000000/next
```

Response:
```json
{
  "message": "Board with ID 00000000-0000-0000-0000-000000000000 not found"
}
```

### 422 Unprocessable Entity - Final State Timeout

For a glider (moving pattern that never stabilizes):

```bash
curl https://localhost:5001/api/boards/{glider-id}/final
```

Response:
```json
{
  "message": "Board did not reach a stable state within 10000 iterations. It may be growing indefinitely or have a very long cycle period."
}
```

## Notes

- All timestamps are in UTC
- Board IDs are UUIDs (v4)
- Replace `{id}` with actual board ID from upload response
- For HTTPS with self-signed cert, use `-k` flag with curl or handle SSL errors appropriately
