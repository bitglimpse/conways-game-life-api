namespace ConwaysGameOfLifeApi.Configuration;

public class GameConfiguration
{
  public const string SectionName = "GameSettings";

  /// <summary>
  /// Maximum board width allowed
  /// </summary>
  public int MaxBoardWidth { get; set; } = 1000;

  /// <summary>
  /// Maximum board height allowed
  /// </summary>
  public int MaxBoardHeight { get; set; } = 1000;

  /// <summary>
  /// Maximum number of iterations to compute when finding final state
  /// </summary>
  public int MaxIterationsForFinalState { get; set; } = 10000;

  /// <summary>
  /// Maximum number of generations ahead that can be requested at once
  /// </summary>
  public int MaxGenerationsAhead { get; set; } = 1000000;

  /// <summary>
  /// Number of historical states to track for cycle detection
  /// </summary>
  public int CycleDetectionHistorySize { get; set; } = 1000;
}
