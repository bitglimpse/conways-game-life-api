using Microsoft.AspNetCore.Mvc;
using ConwaysGameOfLifeApi.Models.DTOs;
using ConwaysGameOfLifeApi.Services;
using FluentValidation;

namespace ConwaysGameOfLifeApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BoardsController : ControllerBase
{
  private readonly IBoardService _boardService;
  private readonly IValidator<UploadBoardRequest> _validator;
  private readonly ILogger<BoardsController> _logger;

  public BoardsController(
      IBoardService boardService,
      IValidator<UploadBoardRequest> validator,
      ILogger<BoardsController> logger)
  {
    _boardService = boardService;
    _validator = validator;
    _logger = logger;
  }

  /// <summary>
  /// Upload a new board state
  /// </summary>
  /// <param name="request">Board data as 2D array of booleans</param>
  /// <returns>Board ID</returns>
  /// <response code="201">Board created successfully</response>
  /// <response code="400">Invalid board data</response>
  [HttpPost]
  [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
  [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
  public async Task<IActionResult> UploadBoard([FromBody] UploadBoardRequest request)
  {
    var validationResult = await _validator.ValidateAsync(request);
    if (!validationResult.IsValid)
    {
      var errors = validationResult.Errors
          .GroupBy(e => e.PropertyName)
          .ToDictionary(
              g => g.Key,
              g => g.Select(e => e.ErrorMessage).ToArray()
          );

      return BadRequest(new ErrorResponse
      {
        Message = "Validation failed",
        Errors = errors
      });
    }

    try
    {
      var boardId = await _boardService.CreateBoardAsync(request);
      _logger.LogInformation("Board {BoardId} created via API", boardId);

      return CreatedAtAction(
          nameof(GetNextState),
          new { id = boardId },
          new { id = boardId }
      );
    }
    catch (ArgumentException ex)
    {
      _logger.LogWarning(ex, "Invalid board data provided");
      return BadRequest(new ErrorResponse { Message = ex.Message });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error creating board");
      return StatusCode(500, new ErrorResponse
      {
        Message = "An error occurred while creating the board"
      });
    }
  }

  /// <summary>
  /// Get the next generation state of a board
  /// </summary>
  /// <param name="id">Board ID</param>
  /// <returns>Next generation board state</returns>
  /// <response code="200">Next state computed successfully</response>
  /// <response code="404">Board not found</response>
  [HttpGet("{id}/next")]
  [ProducesResponseType(typeof(NextStateResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
  public async Task<IActionResult> GetNextState(Guid id)
  {
    try
    {
      var result = await _boardService.GetNextStateAsync(id);
      return Ok(result);
    }
    catch (BoardNotFoundException ex)
    {
      _logger.LogWarning(ex, "Board {BoardId} not found", id);
      return NotFound(new ErrorResponse { Message = ex.Message });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error computing next state for board {BoardId}", id);
      return StatusCode(500, new ErrorResponse
      {
        Message = "An error occurred while computing the next state"
      });
    }
  }

  /// <summary>
  /// Get the board state N generations ahead
  /// </summary>
  /// <param name="id">Board ID</param>
  /// <param name="n">Number of generations to compute ahead</param>
  /// <returns>Board state after N generations</returns>
  /// <response code="200">State computed successfully</response>
  /// <response code="400">Invalid N value</response>
  /// <response code="404">Board not found</response>
  [HttpGet("{id}/generations/{n}")]
  [ProducesResponseType(typeof(NextStateResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
  public async Task<IActionResult> GetNStatesAhead(Guid id, int n)
  {
    if (n <= 0)
    {
      return BadRequest(new ErrorResponse
      {
        Message = "N must be a positive integer"
      });
    }

    try
    {
      var result = await _boardService.GetNStatesAheadAsync(id, n);
      return Ok(result);
    }
    catch (BoardNotFoundException ex)
    {
      _logger.LogWarning(ex, "Board {BoardId} not found", id);
      return NotFound(new ErrorResponse { Message = ex.Message });
    }
    catch (ArgumentException ex)
    {
      _logger.LogWarning(ex, "Invalid N value {N} for board {BoardId}", n, id);
      return BadRequest(new ErrorResponse { Message = ex.Message });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error computing {N} states ahead for board {BoardId}", n, id);
      return StatusCode(500, new ErrorResponse
      {
        Message = "An error occurred while computing the board state"
      });
    }
  }

  /// <summary>
  /// Get the final stable state of a board
  /// </summary>
  /// <param name="id">Board ID</param>
  /// <returns>Final stable board state or timeout error</returns>
  /// <response code="200">Final state found</response>
  /// <response code="404">Board not found</response>
  /// <response code="422">Board did not stabilize within iteration limit</response>
  [HttpGet("{id}/final")]
  [ProducesResponseType(typeof(FinalStateResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
  [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
  public async Task<IActionResult> GetFinalState(Guid id)
  {
    try
    {
      var result = await _boardService.GetFinalStateAsync(id);
      return Ok(result);
    }
    catch (BoardNotFoundException ex)
    {
      _logger.LogWarning(ex, "Board {BoardId} not found", id);
      return NotFound(new ErrorResponse { Message = ex.Message });
    }
    catch (FinalStateTimeoutException ex)
    {
      _logger.LogWarning(ex, "Board {BoardId} did not stabilize", id);
      return UnprocessableEntity(new ErrorResponse { Message = ex.Message });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error computing final state for board {BoardId}", id);
      return StatusCode(500, new ErrorResponse
      {
        Message = "An error occurred while computing the final state"
      });
    }
  }
}
