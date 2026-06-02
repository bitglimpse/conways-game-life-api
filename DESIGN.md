# Design Document: Conway's Game of Life API

## Executive Summary

This document outlines the design decisions, architecture, and trade-offs made in implementing a production-ready RESTful API for Conway's Game of Life.

## Architecture Overview

### Layered Architecture

The application follows a clean, layered architecture pattern:

```
┌─────────────────────────────────────────┐
│     Controllers (HTTP Layer)             │  ← API endpoints, HTTP concerns
├─────────────────────────────────────────┤
│     Services (Business Logic)            │  ← Orchestration, business rules
├─────────────────────────────────────────┤
│     Repositories (Data Access)           │  ← Database operations
├─────────────────────────────────────────┤
│     Game Engine (Core Logic)             │  ← Conway's rules, pure logic
├─────────────────────────────────────────┤
│     Entity Framework Core                │  ← ORM
├─────────────────────────────────────────┤
│     PostgreSQL Database                  │  ← Persistent storage
└─────────────────────────────────────────┘
```

**Rationale:** Separation of concerns enables:
- Independent testing of each layer
- Easy replacement of components (e.g., swap PostgreSQL for SQL Server)
- Clear responsibilities and maintainability

### Dependency Injection

All dependencies are registered in `Program.cs` and injected via constructors:

```csharp
services.AddScoped<IBoardRepository, BoardRepository>();
services.AddScoped<IBoardService, BoardService>();
services.AddSingleton<ConwayGameEngine>();
```

**Rationale:**
- Built-in .NET DI container (no external libraries)
- Testability (easy to mock dependencies)
- Lifetime management (Scoped for per-request, Singleton for stateless)

## Key Design Decisions

### 1. Compute On-Demand vs. Caching

**Decision:** Compute generations on-demand, do not cache intermediate states.

**Options Considered:**
- **Option A:** Compute on-demand (chosen)
- **Option B:** Cache all computed generations in database
- **Option C:** Use Redis for LRU cache of computed states

**Rationale:**
- **Simplicity:** No cache invalidation logic, no stale data concerns
- **Storage:** Avoids exponential storage growth (1M generations = 1M database rows)
- **Time Constraint:** 4-5 hour implementation window favors simpler approach
- **Sufficient Performance:** Computation is fast enough for reasonable board sizes

**Trade-off:** Repeated queries for same generation recompute. Acceptable for exercise; optimize later if needed.

### 2. Cycle Detection Algorithm

**Decision:** Track state history using SHA256 hashing with configurable history size.

**Algorithm:**
```csharp
1. Hash current board state (SHA256)
2. Check if hash exists in history
3. If match found → cycle detected, calculate period
4. Add hash to history (circular buffer, max 1000 states)
```

**Rationale:**
- **Efficiency:** Comparing hashes (256 bits) is faster than deep board comparison
- **Memory:** Bounded history prevents unbounded growth
- **Accuracy:** SHA256 collision probability is negligible

**Alternative Considered:** Floyd's cycle detection (tortoise/hare) - rejected because it requires storing full states, not just hashes, and is harder to debug.

**Limitation:** Cannot detect cycles with period > 1000. This is acceptable as most known oscillators have period < 100.

### 3. Database Choice: PostgreSQL

**Decision:** Use PostgreSQL with JSONB for board storage.

**Schema:**
```sql
CREATE TABLE boards (
    id UUID PRIMARY KEY,
    width INT NOT NULL,
    height INT NOT NULL,
    board_data JSONB NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);
```

**Rationale:**
- **JSONB:** Efficient storage for 2D arrays, supports indexing/querying if needed later
- **PostgreSQL:** Robust, open-source, excellent JSON support
- **UUID:** Globally unique IDs, distributed system friendly

**Alternative Considered:** SQL Server with JSON columns - functionally similar, but PostgreSQL is open-source and has better JSON performance.

### 4. Board State Format

**Decision:** Store as jagged array `bool[][]` in API, convert to/from 2D array `bool[,]` for computation.

**Rationale:**
- **JSON Compatibility:** Jagged arrays serialize naturally to JSON
- **Computation Efficiency:** 2D arrays have better memory locality for nested loops
- **Clear Conversion:** Explicit conversion between formats prevents bugs

**Example:**
```json
{
  "boardData": [
    [true, false, true],
    [false, true, false]
  ]
}
```

### 5. Validation Strategy

**Decision:** Use FluentValidation for input validation.

**Validations:**
- Board not null/empty
- All rows have same width (rectangular grid)
- Dimensions ≤ max configured size (default 1000×1000)

**Rationale:**
- **Declarative:** Validation rules are readable and maintainable
- **Testable:** Validators can be unit tested independently
- **Separation:** Keeps validation logic out of controllers

**Alternative Considered:** Data Annotations - simpler but less flexible for complex rules.

### 6. Error Handling

**Decision:** Global exception middleware + specific exception types.

**HTTP Status Codes:**
- `400 Bad Request` - Invalid input (validation errors)
- `404 Not Found` - Board doesn't exist
- `422 Unprocessable Entity` - Final state timeout (board doesn't stabilize)
- `500 Internal Server Error` - Unexpected errors

**Rationale:**
- **Consistent:** All errors have same JSON structure `{message, errors?}`
- **Semantic:** Status codes follow REST conventions
- **Logging:** Middleware logs all errors for debugging

### 7. Testing Strategy

**Decision:** Comprehensive unit tests + integration tests with in-memory database.

**Unit Tests (27 tests):**
- Conway's rules correctness (underpopulation, survival, overpopulation, reproduction)
- Known patterns (blinker, block, glider)
- Cycle detection logic
- Edge cases (empty boards, single cells)

**Integration Tests (13 tests):**
- Full HTTP pipeline (request → response)
- Database persistence
- Validation errors
- End-to-end workflows

**Rationale:**
- **Confidence:** Core logic (Conway's rules) must be 100% correct
- **Regression Prevention:** Pattern tests catch rule changes
- **Fast Feedback:** In-memory DB makes integration tests fast (< 1s)

**Alternative Considered:** Test containers with real PostgreSQL - more realistic but slower and requires Docker.

### 8. Final State Detection

**Decision:** Detect both static patterns AND oscillators, timeout after configurable iterations.

**Algorithm:**
```
1. Start with initial state, add to history
2. For each generation (up to max iterations):
   a. Compute next generation
   b. Check if all dead → return "static"
   c. Check if state repeats → return "static" or "oscillating" with period
3. If max iterations reached → return 422 error
```

**Rationale:**
- **Completeness:** Most users expect oscillators to be detected as "final"
- **Safety:** Timeout prevents infinite loops on growing patterns (e.g., glider gun)
- **Informative:** Return period for oscillators (useful for analysis)

**Trade-off:** Some patterns (e.g., gliders) will timeout. This is acceptable - we document it and return a clear error.

## Performance Characteristics

### Time Complexity

| Operation | Complexity | Notes |
|-----------|-----------|-------|
| Compute next generation | O(w × h) | w = width, h = height |
| Compute N ahead | O(N × w × h) | Linear in N |
| Find final state | O(M × w × h) | M = iterations to stabilize |
| Cycle detection | O(H) | H = history size (max 1000) |

### Space Complexity

| Component | Complexity | Notes |
|-----------|-----------|-------|
| Board storage | O(w × h) | Per board in database |
| Cycle detection history | O(H × w × h) | Bounded by max history size |
| Computation | O(w × h) | Single next-gen computation |

### Scalability Considerations

**Current Limitations:**
- Single-threaded computation (no parallelism)
- Synchronous endpoints (blocks thread during computation)
- No distributed caching

**How to Scale:**
1. **Horizontal Scaling:** Stateless design allows multiple instances behind load balancer
2. **Async Computation:** For large N, use background jobs (Hangfire/Quartz) + polling
3. **Caching:** Add Redis for frequently accessed boards
4. **Computation Optimization:** Parallelize generation computation for large boards
5. **Database:** Read replicas for GET requests, primary for POST

## Production Readiness

### What's Implemented

✅ Persistent storage (PostgreSQL)  
✅ Input validation  
✅ Error handling  
✅ Logging (structured)  
✅ Configuration management  
✅ Unit + integration tests  

### What's NOT Implemented (Out of Scope)

❌ Authentication/authorization (per requirements)  
❌ Health checks  
❌ CORS support  
❌ API documentation (Swagger/OpenAPI)  
❌ Rate limiting  
❌ Distributed caching  
❌ Monitoring/metrics (e.g., Prometheus)  
❌ Background job processing  
❌ Docker containerization  

### How to Make Fully Production-Ready

1. **Add Authentication:** JWT tokens or OAuth2
2. **Add Rate Limiting:** ASP.NET Core rate limiter middleware
3. **Add Observability:** Application Insights or Datadog
4. **Add Caching:** Redis for board states
5. **Containerize:** Dockerfile + docker-compose for local dev
6. **CI/CD:** GitHub Actions or Azure DevOps pipeline
7. **Infrastructure as Code:** Terraform for cloud deployment

## Trade-offs and Alternatives

### Trade-off 1: Simplicity vs. Performance

**Decision:** Favor simplicity (compute on-demand)  
**Alternative:** Pre-compute and cache  
**Reasoning:** Meets requirements within time constraint; optimization is premature

### Trade-off 2: Storage Format

**Decision:** JSONB in PostgreSQL  
**Alternative:** Binary serialization or separate table for cells  
**Reasoning:** JSONB is human-readable, debuggable, and efficient enough

### Trade-off 3: Testing Database

**Decision:** In-memory provider  
**Alternative:** Real PostgreSQL via test containers  
**Reasoning:** Faster tests, simpler setup, sufficient for exercise

## Extension Points

The design supports future enhancements:

1. **Board History API:** Add endpoints to retrieve all generations
2. **Pattern Library:** Pre-seed database with famous patterns
3. **Visualization:** Add WebSocket endpoint for real-time updates
4. **Analysis Tools:** Add endpoint to detect pattern type (still life, oscillator, spaceship)
5. **Rule Variations:** Support different Conway-like cellular automata rules

## Lessons Learned

### What Went Well
- Layered architecture made testing straightforward
- Repository pattern simplified switching to in-memory DB for tests
- Cycle detection with hashing avoided complex state comparison

### What Could Be Improved
- Could add GraphQL for more flexible querying
- Could use Result<T> pattern instead of throwing exceptions
- Could add more granular logging (trace computation steps)

## Conclusion

This design prioritizes:
- **Correctness:** Comprehensive tests ensure Conway's rules are implemented correctly
- **Maintainability:** Clean architecture and separation of concerns
- **Production-readiness:** Error handling, logging, validation, persistence
- **Pragmatism:** Simple solutions that meet requirements without over-engineering

The architecture is extensible and ready for production deployment with minor additions (auth, monitoring, etc.).
