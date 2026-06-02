# Conway's Game of Life API - Original Requirements

## Objective
Implement a RESTful API for Conway's Game of Life. Your solution should be designed with production readiness in mind.

**Reference:** https://en.wikipedia.org/wiki/Conway%27s_Game_of_Life

## Functional Requirements

The API should include (at a minimum) the following endpoints:

### 1. Upload Board State
- Accept a new board state (2D grid of cells).
- Return a unique identifier for the stored board.

### 2. Get Next State
- Given a board ID, return the next generation state of the board.

### 3. Get N States Ahead
- Given a board ID and a number N, return the board state after N generations.

### 4. Get Final State
- Return the final stable state of the board (i.e., when it no longer changes or cycles).
- If the board does not reach a stable conclusion within a reasonable number of iterations, return a suitable error message.

## Non-Functional Requirements

- The service must **persist board states** so they are not lost if the application is restarted or crashes.
- The code should be **production-ready**:
  - Clean, modular, and testable
  - Includes appropriate error handling and validation
  - Follows C# and .NET best practices
- You **do not need to implement authentication or authorization**.

## Evaluation Criteria

- **Correctness** – Does the API behave as described?
- **Code Quality** – Is the code clean, well-structured, and maintainable?
- **Design & Architecture** – Are design decisions well thought out? Is the code extensible?
- **Production Readiness** – Is the service robust and resilient?
- **Discussion Readiness** – Be prepared to walk us through your design and decisions in a follow-up discussion.

## Estimated Time
This exercise may take 4–5 hours. Manage your time appropriately. We are more interested in quality and thoughtful design than in a perfect or overly complex implementation.

---

## Implementation Status

✅ **All requirements implemented and tested (38 passing tests)**

See [README.md](README.md) for usage guide and [DESIGN.md](DESIGN.md) for architectural decisions.
