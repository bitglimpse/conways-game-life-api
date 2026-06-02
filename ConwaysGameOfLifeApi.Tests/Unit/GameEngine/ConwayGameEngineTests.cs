using ConwaysGameOfLifeApi.GameEngine;
using Xunit;

namespace ConwaysGameOfLifeApi.Tests.Unit.GameEngine;

public class ConwayGameEngineTests
{
  private readonly ConwayGameEngine _engine;

  public ConwayGameEngineTests()
  {
    _engine = new ConwayGameEngine();
  }

  [Fact]
  public void ComputeNextGeneration_EmptyBoard_RemainsEmpty()
  {
    // Arrange
    var state = new bool[3, 3];

    // Act
    var nextState = _engine.ComputeNextGeneration(state);

    // Assert
    for (int i = 0; i < 3; i++)
    {
      for (int j = 0; j < 3; j++)
      {
        Assert.False(nextState[i, j]);
      }
    }
  }

  [Fact]
  public void ComputeNextGeneration_SingleCell_Dies()
  {
    // Arrange - single cell dies from underpopulation
    var state = new bool[3, 3];
    state[1, 1] = true;

    // Act
    var nextState = _engine.ComputeNextGeneration(state);

    // Assert
    Assert.False(nextState[1, 1]);
  }

  [Fact]
  public void ComputeNextGeneration_BlockPattern_RemainsStatic()
  {
    // Arrange - 2x2 block is a static pattern
    var state = new bool[4, 4];
    state[1, 1] = true;
    state[1, 2] = true;
    state[2, 1] = true;
    state[2, 2] = true;

    // Act
    var nextState = _engine.ComputeNextGeneration(state);

    // Assert - block should remain unchanged
    Assert.True(nextState[1, 1]);
    Assert.True(nextState[1, 2]);
    Assert.True(nextState[2, 1]);
    Assert.True(nextState[2, 2]);
  }

  [Fact]
  public void ComputeNextGeneration_BlinkerPattern_Oscillates()
  {
    // Arrange - horizontal line of 3 cells (blinker)
    var state = new bool[5, 5];
    state[2, 1] = true;
    state[2, 2] = true;
    state[2, 3] = true;

    // Act - compute next generation
    var nextState = _engine.ComputeNextGeneration(state);

    // Assert - should become vertical line
    Assert.False(nextState[2, 1]);
    Assert.True(nextState[1, 2]);
    Assert.True(nextState[2, 2]);
    Assert.True(nextState[3, 2]);
    Assert.False(nextState[2, 3]);

    // Act - compute one more generation
    var nextNextState = _engine.ComputeNextGeneration(nextState);

    // Assert - should return to horizontal
    Assert.True(nextNextState[2, 1]);
    Assert.True(nextNextState[2, 2]);
    Assert.True(nextNextState[2, 3]);
  }

  [Fact]
  public void ComputeNextGeneration_Glider_Moves()
  {
    // Arrange - glider pattern
    var state = new bool[6, 6];
    state[1, 2] = true;
    state[2, 3] = true;
    state[3, 1] = true;
    state[3, 2] = true;
    state[3, 3] = true;

    // Act - compute next generation
    var nextState = _engine.ComputeNextGeneration(state);

    // Assert - glider should have moved
    Assert.True(nextState[2, 1]);
    Assert.True(nextState[2, 3]);
    Assert.True(nextState[3, 2]);
    Assert.True(nextState[3, 3]);
    Assert.True(nextState[4, 2]);
  }

  [Fact]
  public void ComputeNextGeneration_Overpopulation_CellDies()
  {
    // Arrange - center cell surrounded by 4+ neighbors dies
    var state = new bool[3, 3];
    state[0, 1] = true;
    state[1, 0] = true;
    state[1, 1] = true; // Center cell
    state[1, 2] = true;
    state[2, 1] = true;

    // Act
    var nextState = _engine.ComputeNextGeneration(state);

    // Assert - center cell should die from overpopulation
    Assert.False(nextState[1, 1]);
  }

  [Fact]
  public void ComputeNextGeneration_Reproduction_DeadCellBecomesAlive()
  {
    // Arrange - dead cell with exactly 3 neighbors becomes alive
    var state = new bool[3, 3];
    state[0, 0] = true;
    state[0, 1] = true;
    state[1, 0] = true;
    // Center cell [1,1] is dead but has 3 neighbors

    // Act
    var nextState = _engine.ComputeNextGeneration(state);

    // Assert - center cell should become alive
    Assert.True(nextState[1, 1]);
  }

  [Fact]
  public void ComputeNextGeneration_Survival_CellWithTwoNeighborsSurvives()
  {
    // Arrange - cell with 2 neighbors survives
    var state = new bool[3, 3];
    state[0, 0] = true;
    state[0, 1] = true;
    state[1, 0] = true;

    // Act
    var nextState = _engine.ComputeNextGeneration(state);

    // Assert - corner cell with 2 neighbors should survive
    Assert.True(nextState[0, 0]);
  }

  [Fact]
  public void ConvertJaggedTo2D_ValidArray_ConvertsCorrectly()
  {
    // Arrange
    var jagged = new bool[][]
    {
            new bool[] { true, false, true },
            new bool[] { false, true, false }
    };

    // Act
    var result = _engine.ConvertJaggedTo2D(jagged);

    // Assert
    Assert.Equal(2, result.GetLength(0)); // height
    Assert.Equal(3, result.GetLength(1)); // width
    Assert.True(result[0, 0]);
    Assert.False(result[0, 1]);
    Assert.True(result[0, 2]);
    Assert.False(result[1, 0]);
    Assert.True(result[1, 1]);
    Assert.False(result[1, 2]);
  }

  [Fact]
  public void ConvertJaggedTo2D_NullArray_ThrowsException()
  {
    // Assert
    Assert.Throws<ArgumentException>(() => _engine.ConvertJaggedTo2D(null!));
  }

  [Fact]
  public void ConvertJaggedTo2D_EmptyArray_ThrowsException()
  {
    // Arrange
    var jagged = new bool[0][];

    // Assert
    Assert.Throws<ArgumentException>(() => _engine.ConvertJaggedTo2D(jagged));
  }

  [Fact]
  public void ConvertJaggedTo2D_RaggedArray_ThrowsException()
  {
    // Arrange - rows with different widths
    var jagged = new bool[][]
    {
            new bool[] { true, false },
            new bool[] { true, false, true } // Different width
    };

    // Assert
    Assert.Throws<ArgumentException>(() => _engine.ConvertJaggedTo2D(jagged));
  }

  [Fact]
  public void Convert2DToJagged_ValidArray_ConvertsCorrectly()
  {
    // Arrange
    var array2D = new bool[2, 3];
    array2D[0, 0] = true;
    array2D[0, 2] = true;
    array2D[1, 1] = true;

    // Act
    var result = _engine.Convert2DToJagged(array2D);

    // Assert
    Assert.Equal(2, result.Length);
    Assert.Equal(3, result[0].Length);
    Assert.True(result[0][0]);
    Assert.False(result[0][1]);
    Assert.True(result[0][2]);
    Assert.False(result[1][0]);
    Assert.True(result[1][1]);
    Assert.False(result[1][2]);
  }

  [Fact]
  public void ComputeNextGeneration_EdgeCells_TreatOutOfBoundsAsDead()
  {
    // Arrange - cell at edge of board
    var state = new bool[3, 3];
    state[0, 0] = true;
    state[0, 1] = true;
    state[1, 0] = true;
    // Corner cell has only 2 neighbors (out of bounds treated as dead)

    // Act
    var nextState = _engine.ComputeNextGeneration(state);

    // Assert - corner cell should have exactly 2 neighbors and survive
    Assert.True(nextState[0, 0]);
  }
}
