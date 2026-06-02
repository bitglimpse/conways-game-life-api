using System.Security.Cryptography;
using System.Text;

namespace ConwaysGameOfLifeApi.GameEngine;

public class CycleDetector
{
  private readonly int _maxHistorySize;
  private readonly List<string> _stateHashes;

  public CycleDetector(int maxHistorySize = 1000)
  {
    _maxHistorySize = maxHistorySize;
    _stateHashes = new List<string>();
  }

  /// <summary>
  /// Adds a state to the history and checks for cycles
  /// </summary>
  /// <param name="state">Current board state</param>
  /// <returns>Tuple of (isCycle, period) where period is generations since last occurrence</returns>
  public (bool IsCycle, int Period) AddStateAndCheckForCycle(bool[,] state)
  {
    string currentHash = ComputeStateHash(state);

    // Check if this state has occurred before
    int previousIndex = _stateHashes.IndexOf(currentHash);

    // Always add to history (before checking cycle to maintain order)
    _stateHashes.Add(currentHash);

    // Limit history size to prevent memory issues
    if (_stateHashes.Count > _maxHistorySize)
    {
      _stateHashes.RemoveAt(0);
      // Adjust previousIndex if we removed an element
      if (previousIndex != -1)
      {
        previousIndex--;
      }
    }

    if (previousIndex != -1 && previousIndex >= 0)
    {
      // Found a cycle! Calculate the period
      // Period is the distance from the previous occurrence to the current position (before we added)
      int period = _stateHashes.Count - 1 - previousIndex;
      return (true, period);
    }

    return (false, 0);
  }

  /// <summary>
  /// Checks if the current state is static (matches the previous state)
  /// </summary>
  public bool IsStatic()
  {
    if (_stateHashes.Count < 2)
      return false;

    return _stateHashes[^1] == _stateHashes[^2]; // Compare last two states
  }

  /// <summary>
  /// Checks if the board is completely dead (all cells are false)
  /// </summary>
  public bool IsAllDead(bool[,] state)
  {
    int height = state.GetLength(0);
    int width = state.GetLength(1);

    for (int i = 0; i < height; i++)
    {
      for (int j = 0; j < width; j++)
      {
        if (state[i, j])
          return false;
      }
    }

    return true;
  }

  /// <summary>
  /// Computes a hash of the board state for efficient comparison
  /// </summary>
  private string ComputeStateHash(bool[,] state)
  {
    int height = state.GetLength(0);
    int width = state.GetLength(1);

    // Build a compact string representation
    StringBuilder sb = new StringBuilder(height * width);
    for (int i = 0; i < height; i++)
    {
      for (int j = 0; j < width; j++)
      {
        sb.Append(state[i, j] ? '1' : '0');
      }
    }

    // Use SHA256 for hashing to avoid collisions
    using (SHA256 sha256 = SHA256.Create())
    {
      byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
      byte[] hash = sha256.ComputeHash(bytes);
      return Convert.ToBase64String(hash);
    }
  }

  public int GetStateCount() => _stateHashes.Count;
}
