using Amazon.DynamoDBv2.Model;
using Dropship.Domain;

namespace Dropship.Repository;

/// <summary>
/// Repositório para gerenciar pedidos no DynamoDB
/// </summary>
public class OrderRepository(DynamoDbRepository dynamoDbRepository, ILogger<OrderRepository> logger)
{
    /// <summary>
    /// Cria um novo pedido no banco de dados
    /// </summary>
    public async Task<OrderDomain> CreateOrderAsync(OrderDomain order)
    {
        logger.LogInformation(
            "Creating order - OrderSn: {OrderSn}, SellerId: {SellerId}, ShopId: {ShopId}",
            order.OrderSn, order.SellerId, order.ShopId);

        try
        {
            var item = order.ToDynamoDb();
            await dynamoDbRepository.PutItemAsync(item);

            logger.LogInformation(
                "Order created successfully - OrderSn: {OrderSn}",
                order.OrderSn);

            return order;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating order - OrderSn: {OrderSn}", order.OrderSn);
            throw;
        }
    }

    /// <summary>
    /// Obtém um pedido pelo OrderSn e SellerId
    /// PK = Orders#{sellerId} | SK = {orderSn}
    /// </summary>
    public async Task<OrderDomain?> GetOrderBySnAsync(string sellerId, string orderSn)
    {
        logger.LogInformation("Getting order - SellerId: {SellerId}, OrderSn: {OrderSn}", sellerId, orderSn);

        try
        {
            var items = await dynamoDbRepository.QueryTableAsync(
                keyConditionExpression: "PK = :pk AND SK = :sk",
                expressionAttributeValues: new Dictionary<string, AttributeValue>
                {
                    { ":pk", new AttributeValue { S = $"Orders#{sellerId}" } },
                    { ":sk", new AttributeValue { S = orderSn } }
                }
            );

            if (items == null || items.Count == 0)
            {
                logger.LogWarning("Order not found - SellerId: {SellerId}, OrderSn: {OrderSn}", sellerId, orderSn);
                return null;
            }

            return OrderMapper.ToDomain(items.First());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting order - SellerId: {SellerId}, OrderSn: {OrderSn}", sellerId, orderSn);
            throw;
        }
    }

    /// <summary>
    /// Obtém todos os pedidos de um vendedor
    /// PK = Orders#{sellerId}
    /// </summary>
    public async Task<List<OrderDomain>> GetOrdersBySellerAsync(string sellerId)
    {
        logger.LogInformation("Getting orders by SellerId - SellerId: {SellerId}", sellerId);

        try
        {
            var items = await dynamoDbRepository.QueryTableAsync(
                keyConditionExpression: "PK = :pk",
                expressionAttributeValues: new Dictionary<string, AttributeValue>
                {
                    { ":pk", new AttributeValue { S = $"Orders#{sellerId}" } }
                }
            );

            if (items == null || items.Count == 0)
            {
                logger.LogWarning("No orders found for seller - SellerId: {SellerId}", sellerId);
                return new List<OrderDomain>();
            }

            var orders = items.Select(OrderMapper.ToDomain).ToList();
            logger.LogInformation("Found {Count} orders for seller - SellerId: {SellerId}", orders.Count, sellerId);

            return orders;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting orders for seller - SellerId: {SellerId}", sellerId);
            throw;
        }
    }

    /// <summary>
    /// Atualiza um pedido existente
    /// </summary>
    public async Task<OrderDomain?> UpdateOrderAsync(string sellerId, string orderSn, OrderDomain order)
    {
        logger.LogInformation("Updating order - SellerId: {SellerId}, OrderSn: {OrderSn}", sellerId, orderSn);

        try
        {
            var existing = await GetOrderBySnAsync(sellerId, orderSn);
            if (existing == null)
            {
                logger.LogWarning("Order not found for update - SellerId: {SellerId}, OrderSn: {OrderSn}", sellerId, orderSn);
                return null;
            }

            order.UpdatedAt = DateTime.UtcNow;
            await dynamoDbRepository.PutItemAsync(order.ToDynamoDb());

            logger.LogInformation("Order updated successfully - SellerId: {SellerId}, OrderSn: {OrderSn}", sellerId, orderSn);
            return order;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating order - SellerId: {SellerId}, OrderSn: {OrderSn}", sellerId, orderSn);
            throw;
        }
    }

    /// <summary>
    /// Deleta um pedido
    /// </summary>
    public async Task<bool> DeleteOrderAsync(string sellerId, string orderSn)
    {
        logger.LogInformation("Deleting order - SellerId: {SellerId}, OrderSn: {OrderSn}", sellerId, orderSn);

        try
        {
            var response = await dynamoDbRepository.DeleteItemAsync(
                new Dictionary<string, AttributeValue>
                {
                    { "PK", new AttributeValue { S = $"Orders#{sellerId}" } },
                    { "SK", new AttributeValue { S = orderSn } }
                }
            );

            logger.LogInformation("Order deleted - SellerId: {SellerId}, OrderSn: {OrderSn}", sellerId, orderSn);
            return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting order - SellerId: {SellerId}, OrderSn: {OrderSn}", sellerId, orderSn);
            throw;
        }
    }
}

