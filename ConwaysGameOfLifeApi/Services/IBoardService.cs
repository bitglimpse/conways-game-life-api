using ConwaysGameOfLifeApi.Models.DTOs;

namespace ConwaysGameOfLifeApi.Services;

public interface IBoardService
{
  Task<Guid> CreateBoardAsync(UploadBoardRequest request);
  Task<NextStateResponse> GetNextStateAsync(Guid boardId);
  Task<NextStateResponse> GetNStatesAheadAsync(Guid boardId, int n);
  Task<FinalStateResponse> GetFinalStateAsync(Guid boardId);
}

public class BoardNotFoundException : Exception
{
  public BoardNotFoundException(Guid boardId)
      : base($"Board with ID {boardId} not found")
  {
  }
}

public class FinalStateTimeoutException : Exception
{
  public FinalStateTimeoutException(int maxIterations)
      : base($"Board did not reach a stable state within {maxIterations} iterations. It may be growing indefinitely or have a very long cycle period.")
  {
  }
}
