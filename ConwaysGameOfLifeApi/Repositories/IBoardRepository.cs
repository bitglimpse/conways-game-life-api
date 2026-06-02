using ConwaysGameOfLifeApi.Models.Domain;

namespace ConwaysGameOfLifeApi.Repositories;

public interface IBoardRepository
{
  Task<Board> SaveAsync(Board board);
  Task<Board?> GetByIdAsync(Guid id);
}
