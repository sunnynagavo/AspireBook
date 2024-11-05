using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace WarehouseAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WarehouseController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public WarehouseController(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetWarehouseStatus()
        {
            var response = await _httpClient.GetAsync("api/WarehouseItems");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return Content(content, "application/json");
        }

        [HttpGet("pending-orders")]
        public async Task<IActionResult> GetPendingOrders()
        {
            var response = await _httpClient.GetAsync("api/Orders?$filter=Status eq 'Pending'");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return Content(content, "application/json");
        }
    }
}
