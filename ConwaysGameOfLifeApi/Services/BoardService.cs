using System.Text.Json;
using ConwaysGameOfLifeApi.Configuration;
using ConwaysGameOfLifeApi.GameEngine;
using ConwaysGameOfLifeApi.Models.Domain;
using ConwaysGameOfLifeApi.Models.DTOs;
using ConwaysGameOfLifeApi.Repositories;
using Microsoft.Extensions.Options;

namespace ConwaysGameOfLifeApi.Services;

public class BoardService : IBoardService
{
  private readonly IBoardRepository _repository;
  private readonly ConwayGameEngine _gameEngine;
  private readonly GameConfiguration _config;
  private readonly ILogger<BoardService> _logger;

  public BoardService(
      IBoardRepository repository,
      ConwayGameEngine gameEngine,
      IOptions<GameConfiguration> config,
      ILogger<BoardService> logger)
  {
    _repository = repository;
    _gameEngine = gameEngine;
    _config = config.Value;
    _logger = logger;
  }

  public async Task<Guid> CreateBoardAsync(UploadBoardRequest request)
  {
    if (request.BoardData == null)
      throw new ArgumentException("BoardData cannot be null");

    // Convert to 2D array for validation
    var state2D = _gameEngine.ConvertJaggedTo2D(request.BoardData);

    var board = new Board
    {
      Id = Guid.NewGuid(),
      Width = request.BoardData[0].Length,
      Height = request.BoardData.Length,
      BoardData = JsonSerializer.Serialize(request.BoardData),
      CreatedAt = DateTime.UtcNow
    };

    await _repository.SaveAsync(board);
    _logger.LogInformation("Created board {BoardId} with dimensions {Width}x{Height}",
        board.Id, board.Width, board.Height);

    return board.Id;
  }

  public async Task<NextStateResponse> GetNextStateAsync(Guid boardId)
  {
    var board = await GetBoardOrThrow(boardId);
    var currentState = DeserializeBoardData(board.BoardData);
    var currentState2D = _gameEngine.ConvertJaggedTo2D(currentState);

    var nextState2D = _gameEngine.ComputeNextGeneration(currentState2D);
    var nextState = _gameEngine.Convert2DToJagged(nextState2D);

    return new NextStateResponse
    {
      BoardData = nextState,
      Generation = 1
    };
  }

  public async Task<NextStateResponse> GetNStatesAheadAsync(Guid boardId, int n)
  {
    if (n <= 0)
      throw new ArgumentException("N must be a positive integer", nameof(n));

    if (n > _config.MaxGenerationsAhead)
      throw new ArgumentException(
          $"N cannot exceed {_config.MaxGenerationsAhead}", nameof(n));

    var board = await GetBoardOrThrow(boardId);
    var currentState = DeserializeBoardData(board.BoardData);
    var state2D = _gameEngine.ConvertJaggedTo2D(currentState);

    _logger.LogInformation("Computing {N} generations ahead for board {BoardId}", n, boardId);

    // Compute N generations
    for (int i = 0; i < n; i++)
    {
      state2D = _gameEngine.ComputeNextGeneration(state2D);
    }

    var resultState = _gameEngine.Convert2DToJagged(state2D);

    return new NextStateResponse
    {
      BoardData = resultState,
      Generation = n
    };
  }

  public async Task<FinalStateResponse> GetFinalStateAsync(Guid boardId)
  {
    var board = await GetBoardOrThrow(boardId);
    var currentState = DeserializeBoardData(board.BoardData);
    var state2D = _gameEngine.ConvertJaggedTo2D(currentState);

    var detector = new CycleDetector(_config.CycleDetectionHistorySize);
    int generation = 0;
    int maxIterations = _config.MaxIterationsForFinalState;

    _logger.LogInformation(
        "Finding final state for board {BoardId} (max {MaxIterations} iterations)",
        boardId, maxIterations);

    // Add initial state
    detector.AddStateAndCheckForCycle(state2D);

    // Check if initially all dead
    if (detector.IsAllDead(state2D))
    {
      _logger.LogInformation("Board {BoardId} is initially all dead", boardId);
      return new FinalStateResponse
      {
        BoardData = _gameEngine.Convert2DToJagged(state2D),
        GenerationCount = 0,
        StateType = "static",
        Period = null
      };
    }

    while (generation < maxIterations)
    {
      generation++;
      state2D = _gameEngine.ComputeNextGeneration(state2D);
      var (isCycle, period) = detector.AddStateAndCheckForCycle(state2D);

      // Check if all cells are dead
      if (detector.IsAllDead(state2D))
      {
        _logger.LogInformation(
            "Board {BoardId} reached all-dead state at generation {Generation}",
            boardId, generation);

        return new FinalStateResponse
        {
          BoardData = _gameEngine.Convert2DToJagged(state2D),
          GenerationCount = generation,
          StateType = "static",
          Period = null
        };
      }

      // Check for cycles (including static)
      if (isCycle)
      {
        string stateType = period == 1 ? "static" : "oscillating";
        _logger.LogInformation(
            "Board {BoardId} reached {StateType} state at generation {Generation} (period: {Period})",
            boardId, stateType, generation, period);

        return new FinalStateResponse
        {
          BoardData = _gameEngine.Convert2DToJagged(state2D),
          GenerationCount = generation,
          StateType = stateType,
          Period = period == 1 ? null : period
        };
      }
    }

    // Timeout - did not reach stable state
    _logger.LogWarning(
        "Board {BoardId} did not reach stable state within {MaxIterations} iterations",
        boardId, maxIterations);

    throw new FinalStateTimeoutException(maxIterations);
  }

  private async Task<Board> GetBoardOrThrow(Guid boardId)
  {
    var board = await _repository.GetByIdAsync(boardId);
    if (board == null)
    {
      throw new BoardNotFoundException(boardId);
    }
    return board;
  }

  private bool[][] DeserializeBoardData(string boardData)
  {
    var data = JsonSerializer.Deserialize<bool[][]>(boardData);
    if (data == null)
      throw new InvalidOperationException("Failed to deserialize board data");
    return data;
  }
}
