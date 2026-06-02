using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConwaysGameOfLifeApi.Data;
using ConwaysGameOfLifeApi.Models.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ConwaysGameOfLifeApi.Tests.Integration.Controllers;

public class BoardsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
  private readonly WebApplicationFactory<Program> _factory;
  private readonly HttpClient _client;
  private static readonly string _databaseName = "TestDatabase_" + Guid.NewGuid();

  public BoardsControllerTests(WebApplicationFactory<Program> factory)
  {
    _factory = factory.WithWebHostBuilder(builder =>
    {
      builder.ConfigureServices(services =>
          {
            // Remove the existing DbContext registration
          var descriptor = services.SingleOrDefault(
                  d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
          if (descriptor != null)
          {
            services.Remove(descriptor);
          }

            // Add in-memory database for testing - use same database name for all requests
          services.AddDbContext<ApplicationDbContext>(options =>
              {
              options.UseInMemoryDatabase(_databaseName);
            });
        });
    });

    _client = _factory.CreateClient();
  }

  [Fact]
  public async Task UploadBoard_ValidBoard_ReturnsCreated()
  {
    // Arrange
    var request = new UploadBoardRequest
    {
      BoardData = new bool[][]
        {
                new bool[] { true, false, true },
                new bool[] { false, true, false },
                new bool[] { true, false, true }
        }
    };

    // Act
    var response = await _client.PostAsJsonAsync("/api/boards", request);

    // Assert
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    var content = await response.Content.ReadFromJsonAsync<JsonElement>();
    Assert.True(content.TryGetProperty("id", out var idProperty));
    Assert.True(Guid.TryParse(idProperty.GetString(), out _));
  }

  [Fact]
  public async Task UploadBoard_NullBoardData_ReturnsBadRequest()
  {
    // Arrange
    var request = new UploadBoardRequest
    {
      BoardData = null
    };

    // Act
    var response = await _client.PostAsJsonAsync("/api/boards", request);

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task UploadBoard_EmptyBoard_ReturnsBadRequest()
  {
    // Arrange
    var request = new UploadBoardRequest
    {
      BoardData = new bool[0][]
    };

    // Act
    var response = await _client.PostAsJsonAsync("/api/boards", request);

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task UploadBoard_RaggedArray_ReturnsBadRequest()
  {
    // Arrange
    var request = new UploadBoardRequest
    {
      BoardData = new bool[][]
        {
                new bool[] { true, false },
                new bool[] { true, false, true } // Different width
        }
    };

    // Act
    var response = await _client.PostAsJsonAsync("/api/boards", request);

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task GetNextState_ExistingBoard_ReturnsNextGeneration()
  {
    // Arrange - upload a blinker pattern
    var uploadRequest = new UploadBoardRequest
    {
      BoardData = new bool[][]
        {
                new bool[] { false, false, false, false, false },
                new bool[] { false, false, false, false, false },
                new bool[] { false, true, true, true, false },
                new bool[] { false, false, false, false, false },
                new bool[] { false, false, false, false, false }
        }
    };

    var uploadResponse = await _client.PostAsJsonAsync("/api/boards", uploadRequest);
    var uploadContent = await uploadResponse.Content.ReadFromJsonAsync<JsonElement>();
    var boardId = uploadContent.GetProperty("id").GetString();

    // Act
    var response = await _client.GetAsync($"/api/boards/{boardId}/next");

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var content = await response.Content.ReadFromJsonAsync<NextStateResponse>();
    Assert.NotNull(content);
    Assert.Equal(1, content.Generation);
    Assert.NotNull(content.BoardData);

    // Verify blinker rotated to vertical
    Assert.True(content.BoardData[1][2]); // Top of vertical line
    Assert.True(content.BoardData[2][2]); // Middle
    Assert.True(content.BoardData[3][2]); // Bottom
  }

  [Fact]
  public async Task GetNextState_NonExistentBoard_ReturnsNotFound()
  {
    // Arrange
    var nonExistentId = Guid.NewGuid();

    // Act
    var response = await _client.GetAsync($"/api/boards/{nonExistentId}/next");

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task GetNStatesAhead_ValidN_ReturnsCorrectGeneration()
  {
    // Arrange - upload a board
    var uploadRequest = new UploadBoardRequest
    {
      BoardData = new bool[][]
        {
                new bool[] { false, true, false },
                new bool[] { false, true, false },
                new bool[] { false, true, false }
        }
    };

    var uploadResponse = await _client.PostAsJsonAsync("/api/boards", uploadRequest);
    var uploadContent = await uploadResponse.Content.ReadFromJsonAsync<JsonElement>();
    var boardId = uploadContent.GetProperty("id").GetString();

    // Act
    var response = await _client.GetAsync($"/api/boards/{boardId}/generations/2");

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var content = await response.Content.ReadFromJsonAsync<NextStateResponse>();
    Assert.NotNull(content);
    Assert.Equal(2, content.Generation);
    Assert.NotNull(content.BoardData);
  }

  [Fact]
  public async Task GetNStatesAhead_NegativeN_ReturnsBadRequest()
  {
    // Arrange
    var uploadRequest = new UploadBoardRequest
    {
      BoardData = new bool[][]
        {
                new bool[] { true, false },
                new bool[] { false, true }
        }
    };

    var uploadResponse = await _client.PostAsJsonAsync("/api/boards", uploadRequest);
    var uploadContent = await uploadResponse.Content.ReadFromJsonAsync<JsonElement>();
    var boardId = uploadContent.GetProperty("id").GetString();

    // Act
    var response = await _client.GetAsync($"/api/boards/{boardId}/generations/-1");

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task GetFinalState_StaticPattern_ReturnsImmediately()
  {
    // Arrange - upload a block (2x2 static pattern)
    var uploadRequest = new UploadBoardRequest
    {
      BoardData = new bool[][]
        {
                new bool[] { false, false, false, false },
                new bool[] { false, true, true, false },
                new bool[] { false, true, true, false },
                new bool[] { false, false, false, false }
        }
    };

    var uploadResponse = await _client.PostAsJsonAsync("/api/boards", uploadRequest);
    var uploadContent = await uploadResponse.Content.ReadFromJsonAsync<JsonElement>();
    var boardId = uploadContent.GetProperty("id").GetString();

    // Act
    var response = await _client.GetAsync($"/api/boards/{boardId}/final");

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var content = await response.Content.ReadFromJsonAsync<FinalStateResponse>();
    Assert.NotNull(content);
    Assert.Equal("static", content.StateType);
    Assert.True(content.GenerationCount <= 1); // Should stabilize immediately
  }

  [Fact]
  public async Task GetFinalState_OscillatingPattern_DetectsCycle()
  {
    // Arrange - upload a blinker (period-2 oscillator)
    var uploadRequest = new UploadBoardRequest
    {
      BoardData = new bool[][]
        {
                new bool[] { false, false, false, false, false },
                new bool[] { false, false, false, false, false },
                new bool[] { false, true, true, true, false },
                new bool[] { false, false, false, false, false },
                new bool[] { false, false, false, false, false }
        }
    };

    var uploadResponse = await _client.PostAsJsonAsync("/api/boards", uploadRequest);
    var uploadContent = await uploadResponse.Content.ReadFromJsonAsync<JsonElement>();
    var boardId = uploadContent.GetProperty("id").GetString();

    // Act
    var response = await _client.GetAsync($"/api/boards/{boardId}/final");

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var content = await response.Content.ReadFromJsonAsync<FinalStateResponse>();
    Assert.NotNull(content);
    Assert.Equal("oscillating", content.StateType);
    Assert.Equal(2, content.Period);
  }

  [Fact]
  public async Task GetFinalState_AllDeadBoard_ReturnsStatic()
  {
    // Arrange - upload an all-dead board
    var uploadRequest = new UploadBoardRequest
    {
      BoardData = new bool[][]
        {
                new bool[] { false, false, false },
                new bool[] { false, false, false },
                new bool[] { false, false, false }
        }
    };

    var uploadResponse = await _client.PostAsJsonAsync("/api/boards", uploadRequest);
    var uploadContent = await uploadResponse.Content.ReadFromJsonAsync<JsonElement>();
    var boardId = uploadContent.GetProperty("id").GetString();

    // Act
    var response = await _client.GetAsync($"/api/boards/{boardId}/final");

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var content = await response.Content.ReadFromJsonAsync<FinalStateResponse>();
    Assert.NotNull(content);
    Assert.Equal("static", content.StateType);
    Assert.Equal(0, content.GenerationCount);
  }

  [Fact]
  public async Task GetFinalState_NonExistentBoard_ReturnsNotFound()
  {
    // Arrange
    var nonExistentId = Guid.NewGuid();

    // Act
    var response = await _client.GetAsync($"/api/boards/{nonExistentId}/final");

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task CompleteWorkflow_UploadGetNextGetFinal_WorksEndToEnd()
  {
    // Arrange & Act - Upload
    var uploadRequest = new UploadBoardRequest
    {
      BoardData = new bool[][]
        {
                new bool[] { false, true, false },
                new bool[] { false, true, false },
                new bool[] { false, true, false }
        }
    };

    var uploadResponse = await _client.PostAsJsonAsync("/api/boards", uploadRequest);
    Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);

    var uploadContent = await uploadResponse.Content.ReadFromJsonAsync<JsonElement>();
    var boardId = uploadContent.GetProperty("id").GetString();

    // Act - Get Next
    var nextResponse = await _client.GetAsync($"/api/boards/{boardId}/next");
    Assert.Equal(HttpStatusCode.OK, nextResponse.StatusCode);

    // Act - Get N Ahead
    var nAheadResponse = await _client.GetAsync($"/api/boards/{boardId}/generations/10");
    Assert.Equal(HttpStatusCode.OK, nAheadResponse.StatusCode);

    // Act - Get Final
    var finalResponse = await _client.GetAsync($"/api/boards/{boardId}/final");
    Assert.Equal(HttpStatusCode.OK, finalResponse.StatusCode);

    var finalContent = await finalResponse.Content.ReadFromJsonAsync<FinalStateResponse>();
    Assert.NotNull(finalContent);
    Assert.Contains(finalContent.StateType, new[] { "static", "oscillating" });
  }
}
