namespace ConwaysGameOfLifeApi.Models.DTOs;

public class UploadBoardRequest
{
  /// <summary>
  /// 2D array representing the board state. true = alive, false = dead
  /// Example: [[true, false, true], [false, true, false]]
  /// </summary>
  public bool[][]? BoardData { get; set; }
}

public class BoardResponse
{
  public Guid Id { get; set; }
  public int Width { get; set; }
  public int Height { get; set; }
  public bool[][] BoardData { get; set; } = Array.Empty<bool[]>();
}

public class NextStateResponse
{
  public bool[][] BoardData { get; set; } = Array.Empty<bool[]>();
  public int Generation { get; set; }
}

public class FinalStateResponse
{
  public bool[][] BoardData { get; set; } = Array.Empty<bool[]>();
  public int GenerationCount { get; set; }
  public string StateType { get; set; } = string.Empty; // "static", "oscillating", or "timeout"
  public int? Period { get; set; } // For oscillating patterns, the period length
}

public class ErrorResponse
{
  public string Message { get; set; } = string.Empty;
  public Dictionary<string, string[]>? Errors { get; set; }
}
