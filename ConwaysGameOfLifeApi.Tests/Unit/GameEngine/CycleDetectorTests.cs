using ConwaysGameOfLifeApi.GameEngine;
using Xunit;

namespace ConwaysGameOfLifeApi.Tests.Unit.GameEngine;

public class CycleDetectorTests
{
  [Fact]
  public void AddStateAndCheckForCycle_NewState_ReturnsNoCycle()
  {
    // Arrange
    var detector = new CycleDetector();
    var state = new bool[3, 3];
    state[1, 1] = true;

    // Act
    var (isCycle, period) = detector.AddStateAndCheckForCycle(state);

    // Assert
    Assert.False(isCycle);
    Assert.Equal(0, period);
  }

  [Fact]
  public void AddStateAndCheckForCycle_RepeatingState_DetectsCycle()
  {
    // Arrange
    var detector = new CycleDetector();
    var state1 = new bool[3, 3];
    state1[1, 1] = true;

    var state2 = new bool[3, 3];
    state2[1, 2] = true;

    // Act
    detector.AddStateAndCheckForCycle(state1);
    detector.AddStateAndCheckForCycle(state2);
    var (isCycle, period) = detector.AddStateAndCheckForCycle(state1); // Repeat state1

    // Assert
    Assert.True(isCycle);
    Assert.Equal(2, period); // Period is 2 (cycles between state1 and state2)
  }

  [Fact]
  public void AddStateAndCheckForCycle_ImmediateRepeat_DetectsPeriodOne()
  {
    // Arrange
    var detector = new CycleDetector();
    var state = new bool[3, 3];
    state[1, 1] = true;

    // Act
    detector.AddStateAndCheckForCycle(state);
    var (isCycle, period) = detector.AddStateAndCheckForCycle(state); // Immediate repeat

    // Assert
    Assert.True(isCycle);
    Assert.Equal(1, period);
  }

  [Fact]
  public void IsStatic_LessThanTwoStates_ReturnsFalse()
  {
    // Arrange
    var detector = new CycleDetector();
    var state = new bool[3, 3];

    // Act
    detector.AddStateAndCheckForCycle(state);
    var isStatic = detector.IsStatic();

    // Assert
    Assert.False(isStatic);
  }

  [Fact]
  public void IsStatic_SameLastTwoStates_ReturnsTrue()
  {
    // Arrange
    var detector = new CycleDetector();
    var state = new bool[3, 3];
    state[1, 1] = true;

    // Act
    detector.AddStateAndCheckForCycle(state);
    detector.AddStateAndCheckForCycle(state); // Add same state again
    var isStatic = detector.IsStatic();

    // Assert
    Assert.True(isStatic);
  }

  [Fact]
  public void IsStatic_DifferentLastTwoStates_ReturnsFalse()
  {
    // Arrange
    var detector = new CycleDetector();
    var state1 = new bool[3, 3];
    state1[1, 1] = true;

    var state2 = new bool[3, 3];
    state2[1, 2] = true;

    // Act
    detector.AddStateAndCheckForCycle(state1);
    detector.AddStateAndCheckForCycle(state2);
    var isStatic = detector.IsStatic();

    // Assert
    Assert.False(isStatic);
  }

  [Fact]
  public void IsAllDead_EmptyBoard_ReturnsTrue()
  {
    // Arrange
    var detector = new CycleDetector();
    var state = new bool[3, 3];

    // Act
    var isAllDead = detector.IsAllDead(state);

    // Assert
    Assert.True(isAllDead);
  }

  [Fact]
  public void IsAllDead_BoardWithLiveCells_ReturnsFalse()
  {
    // Arrange
    var detector = new CycleDetector();
    var state = new bool[3, 3];
    state[1, 1] = true;

    // Act
    var isAllDead = detector.IsAllDead(state);

    // Assert
    Assert.False(isAllDead);
  }

  [Fact]
  public void AddStateAndCheckForCycle_BlinkerPattern_DetectsPeriodTwo()
  {
    // Arrange
    var detector = new CycleDetector();
    var engine = new ConwayGameEngine();

    // Horizontal blinker
    var state = new bool[5, 5];
    state[2, 1] = true;
    state[2, 2] = true;
    state[2, 3] = true;

    // Act
    detector.AddStateAndCheckForCycle(state);

    // Compute next generation (vertical)
    var nextState = engine.ComputeNextGeneration(state);
    detector.AddStateAndCheckForCycle(nextState);

    // Compute next generation (back to horizontal)
    var thirdState = engine.ComputeNextGeneration(nextState);
    var (isCycle, period) = detector.AddStateAndCheckForCycle(thirdState);

    // Assert
    Assert.True(isCycle);
    Assert.Equal(2, period); // Blinker has period 2
  }

  [Fact]
  public void AddStateAndCheckForCycle_HistoryLimit_RemovesOldStates()
  {
    // Arrange
    var maxHistory = 5;
    var detector = new CycleDetector(maxHistory);

    // Act - add more states than history size
    for (int i = 0; i < 10; i++)
    {
      var state = new bool[3, 3];
      state[0, i % 3] = true; // Different states
      detector.AddStateAndCheckForCycle(state);
    }

    // Assert - history should be limited
    Assert.True(detector.GetStateCount() <= maxHistory);
  }

  [Fact]
  public void AddStateAndCheckForCycle_DifferentStates_DoesNotDetectCycle()
  {
    // Arrange
    var detector = new CycleDetector();

    // Act - add several different states
    for (int i = 0; i < 5; i++)
    {
      var state = new bool[3, 3];
      state[i % 3, i / 3] = true; // Different position each time
      var (isCycle, _) = detector.AddStateAndCheckForCycle(state);

      // Assert
      Assert.False(isCycle);
    }
  }
}
