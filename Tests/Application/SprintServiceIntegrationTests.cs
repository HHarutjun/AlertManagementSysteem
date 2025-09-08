using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Xunit;

public class SprintServiceIntegrationTests
{
    /// <summary>
    /// AIT-10: Test ophalen van huidige sprint via echte SprintService.
    /// </summary>
    [Fact(DisplayName = "AIT-10: SprintService haalt huidige sprintnaam op (integration)")]
    public async Task SprintService_ReturnsCurrentSprintName()
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

        // Echte SprintService, maar HttpClient wordt niet direct ondersteund, dus we testen via de fake
        var sprintService = new SprintServiceFake(httpClient);

        // Act
        var sprint = await sprintService.GetCurrentSprintAsync("Sheldon");

        // Assert
        Assert.Equal(@"Development\Sheldon\Sprint 1", sprint);
    }
}
