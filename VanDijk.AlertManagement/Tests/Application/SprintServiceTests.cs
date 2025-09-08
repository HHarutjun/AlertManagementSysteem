using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Xunit;

public class SprintServiceTests
{
    /// <summary>
    /// AUT-29: Test ophalen van huidige sprint (mocked response)
    /// </summary>
    [Fact(DisplayName = "AUT-29: GetCurrentSprintAsync retourneert juiste sprintnaam")]
    public async Task GetCurrentSprintAsync_ReturnsSprintName()
    {
        // Arrange
        var responseJson = "{ \"value\": [ { \"path\": \"Development\\\\Sheldon\\\\Sprint 1\" } ] }";
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseJson)
            });

        var httpClient = new HttpClient(handler.Object);
        var service = new SprintServiceFake(httpClient);
        Console.WriteLine($"[Test] Sprint returned: '{responseJson}'");


        // Act
        var sprint = await service.GetCurrentSprintAsync("Sheldon");

        // Debug output
        Console.WriteLine($"[Test] Sprint returned: '{sprint}'");

        // Assert
        Assert.Equal(@"Development\Sheldon\Sprint 1", sprint);
    }

    /// <summary>
    /// AUT-30: Test exception bij geen sprint gevonden
    /// </summary>
    [Fact(DisplayName = "AUT-30: Exception bij geen sprint gevonden")]
    public async Task GetCurrentSprintAsync_NoSprintFound_Throws()
    {
        // Arrange
        var responseJson = @"{ ""value"": [] }";
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseJson)
            });

        var httpClient = new HttpClient(handler.Object);
        var service = new SprintServiceFake(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => service.GetCurrentSprintAsync("Sheldon"));
    }

    /// <summary>
    /// AUT-31: Test exception bij fout response
    /// </summary>
    [Fact(DisplayName = "AUT-31: Exception bij fout response van Azure DevOps")]
    public async Task GetCurrentSprintAsync_ErrorResponse_Throws()
    {
        // Arrange
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("Bad request")
            });

        var httpClient = new HttpClient(handler.Object);
        var service = new SprintServiceFake(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => service.GetCurrentSprintAsync("Sheldon"));
    }
}
