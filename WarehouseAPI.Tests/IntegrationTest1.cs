using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Text.Json;
using Moq;
using Moq.Protected;

namespace WarehouseAPI.Tests
{
    [TestClass]
    public class IntegrationTest1
    {
        [TestMethod]
        public async Task GetWebResourceRootReturnsOkStatusCode()
        {
            // Arrange
            var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Eshop_AppHost>();
            appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
            {
                clientBuilder.AddStandardResilienceHandler();
            });
            await using var app = await appHost.BuildAsync();
            var resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();
            await app.StartAsync();

            // Act
            var httpClient = app.CreateHttpClient("frontend-react-app");
            await resourceNotificationService.WaitForResourceAsync("frontend-react-app", KnownResourceStates.Running).WaitAsync(TimeSpan.FromSeconds(30));
            var response = await httpClient.GetAsync("/");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task GetWarehouseStatus_ReturnsAvailableItems_WhenNoPendingOrders()
        {
            // Arrange
            var items = new List<WarehouseItem>
            {
                new(1, "ItemA", 10, DateTime.UtcNow),
                new(2, "ItemB", 5, DateTime.UtcNow)
            };
            var orders = new List<Order>(); // No pending orders

            var httpClient = CreateMockHttpClient(items, orders);

            var client = new WarehouseClient(httpClient);

            // Act
            var result = await client.GetWarehouseStatus();

            // Assert
            Assert.AreEqual(2, result.Length);
            Assert.IsTrue(result.Any(i => i.ItemID == 1));
            Assert.IsTrue(result.Any(i => i.ItemID == 2));
        }

        [TestMethod]
        public async Task GetWarehouseStatus_ExcludesItemsWithPendingOrders()
        {
            // Arrange
            var items = new List<WarehouseItem>
            {
                new(1, "ItemA", 10, DateTime.UtcNow),
                new(2, "ItemB", 5, DateTime.UtcNow)
            };
            var orders = new List<Order>
            {
                new(100, "Cust", 1, 1, "Pending", DateTime.UtcNow, DateTime.UtcNow)
            };

            var httpClient = CreateMockHttpClient(items, orders);

            var client = new WarehouseClient(httpClient);

            // Act
            var result = await client.GetWarehouseStatus();

            // Assert
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(2, result[0].ItemID);
        }

        private static HttpClient CreateMockHttpClient(List<WarehouseItem> items, List<Order> orders)
        {
            var handlerMock = new Mock<HttpMessageHandler>();

            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("WarehouseItems")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(new DABResponse<WarehouseItem>(items)))
                });

            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("Orders")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(new DABResponse<Order>(orders)))
                });

            return new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost/")
            };
        }

    }
}
