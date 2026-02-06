using Amazon.DynamoDBv2.Model;
using Dropship.Domain;
using Dropship.Repository;
using Dropship.Responses;

namespace Dropship.Services;

public class PaymentService(DynamoDbRepository repository, SupplierRepository supplierRepository)
{
    public async Task<List<PaymentSupplierResponse>> GetPendingPaymentsAsync(string sellerId)
    {
        var results = await repository.QueryTableAsync(
            "PK = :pk AND begins_with(SK, :sk)",
            expressionAttributeValues: new Dictionary<string, AttributeValue>
            {
                {":pk", new AttributeValue { S = $"PaymentQueue#Seller#{sellerId}" }},
                {":sk", new AttributeValue { S = "PaymentStatus#Pending#" }}
            });

        var payments = results.Select(PaymentMapper.ToDomain).ToList();
        
        var groupedBySupplier = payments.GroupBy(p => p.SupplierId).ToList();
        var supplierResponses = new List<PaymentSupplierResponse>();

        foreach (var supplierGroup in groupedBySupplier)
        {
            var supplier = await supplierRepository.GetSupplierAsync(supplierGroup.Key);
            var supplierPayments = supplierGroup.ToList();
            
            var orders = supplierPayments.Select(p => new PaymentOrderResponse
            {
                Sku = p.Sku ?? "",
                Qty = p.Quantity,
                UnitPrice = p.Value,
                OrderId = p.OrderSn ?? "",
                Date = p.CreatedAt.ToString("yyyy-MM-dd"),
                Status = "pending"
            }).ToList();

            supplierResponses.Add(new PaymentSupplierResponse
            {
                Id = supplierGroup.Key,
                Name = supplier?.Name ?? "Unknown Supplier",
                TotalDue = supplierPayments.Sum(p => p.Value),
                ProductsCount = supplierPayments.Count,
                Orders = orders
            });
        }

        return supplierResponses;
    }
}