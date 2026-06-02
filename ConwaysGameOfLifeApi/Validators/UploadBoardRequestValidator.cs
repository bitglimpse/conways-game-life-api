using FluentValidation;
using ConwaysGameOfLifeApi.Models.DTOs;
using ConwaysGameOfLifeApi.Configuration;
using Microsoft.Extensions.Options;

namespace ConwaysGameOfLifeApi.Validators;

public class UploadBoardRequestValidator : AbstractValidator<UploadBoardRequest>
{
  public UploadBoardRequestValidator(IOptions<GameConfiguration> config)
  {
    var settings = config.Value;

    RuleFor(x => x.BoardData)
        .NotNull()
        .WithMessage("BoardData is required");

    RuleFor(x => x.BoardData)
        .Must(board => board != null && board.Length > 0)
        .WithMessage("Board must have at least one row")
        .When(x => x.BoardData != null);

    RuleFor(x => x.BoardData)
        .Must(board => board![0] != null && board[0].Length > 0)
        .WithMessage("Board must have at least one column")
        .When(x => x.BoardData != null && x.BoardData.Length > 0);

    RuleFor(x => x.BoardData)
        .Must(board => board!.Length <= settings.MaxBoardHeight)
        .WithMessage($"Board height cannot exceed {settings.MaxBoardHeight}")
        .When(x => x.BoardData != null && x.BoardData.Length > 0);

    RuleFor(x => x.BoardData)
        .Must(board => board![0].Length <= settings.MaxBoardWidth)
        .WithMessage($"Board width cannot exceed {settings.MaxBoardWidth}")
        .When(x => x.BoardData != null && x.BoardData.Length > 0);

    RuleFor(x => x.BoardData)
        .Must(HaveConsistentRowWidths)
        .WithMessage("All rows must have the same width")
        .When(x => x.BoardData != null && x.BoardData.Length > 0);
  }

  private bool HaveConsistentRowWidths(bool[][]? board)
  {
    if (board == null || board.Length == 0)
      return true;

    int expectedWidth = board[0]?.Length ?? 0;

    foreach (var row in board)
    {
      if (row == null || row.Length != expectedWidth)
        return false;
    }

    return true;
  }
}
