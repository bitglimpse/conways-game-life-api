namespace ConwaysGameOfLifeApi.Models.Domain;

public class Board
{
  /// <summary>
  /// Unique identifier for the board
  /// </summary>
  public Guid Id { get; set; }

  /// <summary>
  /// Board width (number of columns)
  /// </summary>
  public int Width { get; set; }

  /// <summary>
  /// Board height (number of rows)
  /// </summary>
  public int Height { get; set; }

  /// <summary>
  /// Board state stored as JSON. Format: 2D array of booleans [[true, false], [false, true]]
  /// </summary>
  public string BoardData { get; set; } = string.Empty;

  /// <summary>
  /// When this board was created
  /// </summary>
  public DateTime CreatedAt { get; set; }
}
