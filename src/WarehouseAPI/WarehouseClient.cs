using System.Linq;

namespace WarehouseAPI;

public class WarehouseClient(HttpClient httpClient)
{
    public async Task<WarehouseItem[]> GetWarehouseStatus(CancellationToken cancellationToken = default)
    {
        List<WarehouseItem> items = new();
        var response = await httpClient.GetFromJsonAsync<DABResponse<WarehouseItem>>("api/WarehouseItems", cancellationToken: cancellationToken);
        var pendingOrders = await httpClient.GetFromJsonAsync<DABResponse<Order>>("api/Orders?$filter=Status eq 'Pending'", cancellationToken: cancellationToken);
        if (response != null && pendingOrders != null)
        {
            items.AddRange(response.Value.Where(item => !pendingOrders.Value.Select(o => o.ItemID).Contains(item.ItemID)));
        }
        return items.ToArray();
    }
}

public record DABResponse<T>(List<T> Value);

public record WarehouseItem(int ItemID, string ItemName, int Stock, DateTime LastUpdated);
public record Order(int OrderID, string CustomerName, int ItemID, int Quantity, string Status, DateTime OrderDate, DateTime LastUpdated);