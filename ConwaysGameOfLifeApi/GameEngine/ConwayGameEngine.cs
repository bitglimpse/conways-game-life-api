namespace ConwaysGameOfLifeApi.GameEngine;

public class ConwayGameEngine
{
  /// <summary>
  /// Computes the next generation of the board based on Conway's Game of Life rules
  /// </summary>
  /// <param name="currentState">Current board state (true = alive, false = dead)</param>
  /// <returns>Next generation board state</returns>
  public bool[,] ComputeNextGeneration(bool[,] currentState)
  {
    int height = currentState.GetLength(0);
    int width = currentState.GetLength(1);
    bool[,] nextState = new bool[height, width];

    for (int row = 0; row < height; row++)
    {
      for (int col = 0; col < width; col++)
      {
        int liveNeighbors = CountLiveNeighbors(currentState, row, col);
        bool isAlive = currentState[row, col];

        // Apply Conway's rules:
        // 1. Any live cell with 2-3 live neighbors survives
        // 2. Any dead cell with exactly 3 live neighbors becomes alive
        // 3. All other cells die or stay dead
        if (isAlive)
        {
          nextState[row, col] = liveNeighbors == 2 || liveNeighbors == 3;
        }
        else
        {
          nextState[row, col] = liveNeighbors == 3;
        }
      }
    }

    return nextState;
  }

  /// <summary>
  /// Counts the number of live neighbors for a cell
  /// </summary>
  private int CountLiveNeighbors(bool[,] state, int row, int col)
  {
    int height = state.GetLength(0);
    int width = state.GetLength(1);
    int count = 0;

    // Check all 8 neighbors
    for (int dr = -1; dr <= 1; dr++)
    {
      for (int dc = -1; dc <= 1; dc++)
      {
        // Skip the cell itself
        if (dr == 0 && dc == 0)
          continue;

        int newRow = row + dr;
        int newCol = col + dc;

        // Check bounds (treat out-of-bounds as dead)
        if (newRow >= 0 && newRow < height && newCol >= 0 && newCol < width)
        {
          if (state[newRow, newCol])
            count++;
        }
      }
    }

    return count;
  }

  /// <summary>
  /// Converts a jagged array (bool[][]) to a 2D array (bool[,])
  /// </summary>
  public bool[,] ConvertJaggedTo2D(bool[][] jagged)
  {
    if (jagged == null || jagged.Length == 0)
      throw new ArgumentException("Board data cannot be null or empty", nameof(jagged));

    int height = jagged.Length;
    int width = jagged[0]?.Length ?? 0;

    if (width == 0)
      throw new ArgumentException("Board must have at least one column", nameof(jagged));

    // Validate all rows have same width
    for (int i = 0; i < height; i++)
    {
      if (jagged[i] == null || jagged[i].Length != width)
        throw new ArgumentException("All rows must have the same width", nameof(jagged));
    }

    bool[,] result = new bool[height, width];
    for (int i = 0; i < height; i++)
    {
      for (int j = 0; j < width; j++)
      {
        result[i, j] = jagged[i][j];
      }
    }

    return result;
  }

  /// <summary>
  /// Converts a 2D array (bool[,]) to a jagged array (bool[][])
  /// </summary>
  public bool[][] Convert2DToJagged(bool[,] array)
  {
    int height = array.GetLength(0);
    int width = array.GetLength(1);
    bool[][] result = new bool[height][];

    for (int i = 0; i < height; i++)
    {
      result[i] = new bool[width];
      for (int j = 0; j < width; j++)
      {
        result[i][j] = array[i, j];
      }
    }

    return result;
  }
}
