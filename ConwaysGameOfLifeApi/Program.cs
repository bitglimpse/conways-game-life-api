using ConwaysGameOfLifeApi.Configuration;
using ConwaysGameOfLifeApi.Data;
using ConwaysGameOfLifeApi.GameEngine;
using ConwaysGameOfLifeApi.Middleware;
using ConwaysGameOfLifeApi.Repositories;
using ConwaysGameOfLifeApi.Services;
using ConwaysGameOfLifeApi.Validators;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add configuration
builder.Services.Configure<GameConfiguration>(
    builder.Configuration.GetSection(GameConfiguration.SectionName));

// Add database context
var databaseProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "InMemory";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (databaseProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        options.UseNpgsql(connectionString);
    }
    else
    {
        options.UseInMemoryDatabase("ConwaysGameOfLife");
    }
});

// Add controllers
builder.Services.AddControllers();

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<UploadBoardRequestValidator>();

// Register application services
builder.Services.AddScoped<IBoardRepository, BoardRepository>();
builder.Services.AddScoped<IBoardService, BoardService>();
builder.Services.AddSingleton<ConwayGameEngine>();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.MapControllers();

app.Run();

// Make the implicit Program class public for integration tests
public partial class Program { }
