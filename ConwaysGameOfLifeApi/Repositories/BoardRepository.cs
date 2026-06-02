using ConwaysGameOfLifeApi.Data;
using ConwaysGameOfLifeApi.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace ConwaysGameOfLifeApi.Repositories;

public class BoardRepository : IBoardRepository
{
  private readonly ApplicationDbContext _context;
  private readonly ILogger<BoardRepository> _logger;

  public BoardRepository(ApplicationDbContext context, ILogger<BoardRepository> logger)
  {
    _context = context;
    _logger = logger;
  }

  public async Task<Board> SaveAsync(Board board)
  {
    try
    {
      _context.Boards.Add(board);
      await _context.SaveChangesAsync();
      _logger.LogInformation("Board {BoardId} saved successfully", board.Id);
      return board;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error saving board {BoardId}", board.Id);
      throw;
    }
  }

  public async Task<Board?> GetByIdAsync(Guid id)
  {
    try
    {
      var board = await _context.Boards
          .AsNoTracking()
          .FirstOrDefaultAsync(b => b.Id == id);

      if (board == null)
      {
        _logger.LogWarning("Board {BoardId} not found", id);
      }

      return board;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving board {BoardId}", id);
      throw;
    }
  }
}
