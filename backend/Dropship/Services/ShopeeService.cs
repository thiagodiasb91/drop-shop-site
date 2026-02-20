using Amazon.SQS;
using Amazon.SQS.Model;
using System.Text.Json;
using Dropship.Repository;
using Dropship.Domain;
using Dropship.Requests;

namespace Dropship.Services;

/// <summary>
/// Serviço para processar eventos de Shopee
/// </summary>
public class ShopeeService(
    SellerRepository sellerRepository,
    UserRepository userRepository,
    ShopeeApiService shopeeApiService,
    ProductSellerRepository productSellerRepository,
    IAmazonSQS sqsClient,
    ILogger<ShopeeService> logger,
    ProductRepository productRepository)
{
    private const string SqsQueueUrl = "https://sqs.us-east-1.amazonaws.com/511758682977/shoppe-new-order-received-queue.fifo";
    
    /// <summary>
    /// Autentica com Shopee usando o authorization code e armazena os tokens
    /// Verifica se o Seller já existe pelo shop_id, se não existir cria um novo
    /// Atualiza o usuário com o resource_id do Seller
    /// </summary>
    public async Task AuthenticateShopAsync(string code, long shopId, string email)
    {
        logger.LogInformation("Authenticating shop with Shopee - ShopId: {ShopId}, Email: {Email}", shopId, email);

        try
        {
            // Validar se o usuário existe
            var user = await userRepository.GetUser(email);
            if (user == null)
            {
                logger.LogWarning("User not found - Email: {Email}", email);
                throw new InvalidOperationException($"User with email {email} not found");
            }

            // Obter tokens da API Shopee
            var accessToken = await shopeeApiService.GetCachedAccessTokenAsync(shopId, code);
            
            var existingSeller = await sellerRepository.GetSellerByShopIdAsync(shopId);
            
            SellerDomain seller;
            if (existingSeller != null)
            {
                logger.LogInformation("Seller already exists - ShopId: {ShopId}, SellerId: {SellerId}", 
                    shopId, existingSeller.SellerId);
                seller = existingSeller;
            }
            else
            {
                // Criar novo Seller
                var sellerId = Guid.NewGuid().ToString();
                seller = new SellerDomain
                {
                    SellerId = sellerId,
                    SellerName = $"Shop_{shopId}",
                    ShopId = shopId,
                    Marketplace = "shopee"
                };

                var createdSeller = await sellerRepository.CreateSellerAsync(seller);
                logger.LogInformation("Seller created successfully - SellerId: {SellerId}, ShopId: {ShopId}", 
                    createdSeller.SellerId, shopId);
                
                seller = createdSeller;
            }

            // Atualizar usuário com o resource_id (sellerId)
            user.ResourceId = seller.SellerId;
            await userRepository.UpdateUserAsync(user);
            logger.LogInformation("User updated with resource_id - Email: {Email}, ResourceId: {ResourceId}", 
                email, seller.SellerId);

            logger.LogInformation("Shop authenticated successfully - ShopId: {ShopId}, Email: {Email}, SellerId: {SellerId}", 
                shopId, email, seller.SellerId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error authenticating shop - ShopId: {ShopId}, Email: {Email}", shopId, email);
            throw;
        }
    }

    /// <summary>
    /// Processa ordem recebida do Shopee
    /// Verifica se a loja existe e envia para fila SQS
    /// </summary>
    public async Task<bool> ProcessOrderReceivedAsync(long shopId, string orderSn, string status)
    {
        logger.LogInformation("Processing order received - OrderSn: {OrderSn}, Status: {Status}, ShopId: {ShopId}", 
            orderSn, status, shopId);

        try
        {
            // Verificar se a loja existe
            var shopExists = await sellerRepository.VerifyIfShopExistsAsync(shopId);
            if (!shopExists)
            {
                logger.LogWarning("Shop not found: {ShopId}", shopId);
                throw new InvalidOperationException($"Shop {shopId} not found");
            }

            // Enviar mensagem para SQS
            await SendOrderToSqsAsync(orderSn, status, shopId.ToString());

            logger.LogInformation("Order processed successfully - OrderSn: {OrderSn}", orderSn);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing order: {OrderSn}", orderSn);
            throw;
        }
    }

    /// <summary>
    /// Envia mensagem de ordem para fila SQS FIFO
    /// </summary>
    private async Task SendOrderToSqsAsync(string orderSn, string status, string shopId)
    {
        logger.LogInformation("Sending order to SQS - OrderSn: {OrderSn}, ShopId: {ShopId}", orderSn, shopId);

        try
        {
            var message = new
            {
                ordersn = orderSn,
                status = status,
                shop_id = shopId
            };

            var messageBody = JsonSerializer.Serialize(message);
            var messageGroupId = $"{shopId}-{orderSn}";

            var sendMessageRequest = new SendMessageRequest
            {
                QueueUrl = SqsQueueUrl,
                MessageBody = messageBody,
                MessageGroupId = messageGroupId
            };

            var response = await sqsClient.SendMessageAsync(sendMessageRequest);

            logger.LogInformation("Message sent to SQS - MessageId: {MessageId}, GroupId: {GroupId}", 
                response.MessageId, messageGroupId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending message to SQS - OrderSn: {OrderSn}", orderSn);
            throw;
        }
    }

    public async Task UploadProduct(ProductSellerDomain productSeller, List<ProductSkuSellerDomain> productsSkuSellers)
    {
        logger.LogInformation("Uploading product - ProductId: {ProductId}, SellerId: {SellerId}, SkuCount: {SkuCount}",
            productSeller.ProductId, productSeller.SellerId, productsSkuSellers.Count);

        try
        {
            var productDefinition = await productRepository.GetProductByIdAsync(productSeller.ProductId);
            var productImages = await productRepository.GetImagesByProductIdAsync(productSeller.ProductId);

            var imagesId = productImages.Select(x => x.ImageId).Distinct().ToArray();
            
            var request = new
            {
                original_price = productSeller.Price,
                description = productDefinition.Description,
                weight = 0.3m,
                item_name = productDefinition.Name,
                dimension = new
                {
                    package_length = 10,
                    package_width = 20,
                    package_height = 20
                },
                logistic_info = new[]
                {
                    new
                    {
                        logistic_id = 91008,
                        enabled = true
                    }
                },

                category_id = productDefinition.CategoryId,

                image = new
                {
                    image_id_list = imagesId.Take(10).ToArray()
                },

                brand = new
                {
                    brand_id = 0,
                    original_brand_name = "No Brand"
                },

                condition = "NEW",
                item_dangerous = 0,

                seller_stock = new[]
                {
                    new
                    {
                        stock = 0
                    }
                }
            };
            
            logger.LogDebug("Creating product on Shopee - ProductId: {ProductId}", productSeller.ProductId);
            var productApiResponse = await shopeeApiService.AddItemAsync(productSeller.StoreId, request);

            var itemId = productApiResponse.RootElement.GetProperty("response")
                                           .GetProperty("item_id")
                                           .GetInt64();

            productSeller.MarketplaceItemId = itemId;
            
            await productSellerRepository.UpdateMarketplaceItemIdAsync(productSeller);
            logger.LogInformation("Product created - ItemId: {ItemId}, ProductId: {ProductId}", itemId, productSeller.ProductId);

            // Agrupar SKUs por cores e tamanhos
            var colors = productsSkuSellers.Select( x=> x.Color).Distinct().ToList();
            var sizes = productsSkuSellers.Select( x=> x.Size).Distinct().ToList();
          
            // Construir variações para Shopee
            var standardiseTierVariation = new[]
            {
                new
                {
                    variation_id = 0,
                    variation_group_id = 0,
                    variation_name = "Cor",
                    variation_option_list = colors.Select(color => new
                    {
                        variation_option_id = 0,
                        variation_option_name = color,
                        image_id = productImages.FirstOrDefault(img => img.Color == color)?.ImageId ?? ""
                    }).ToArray()
                },
                new
                {
                    variation_id = 0,
                    variation_group_id = 0,
                    variation_name = "Tamanho",
                    variation_option_list = sizes.Select(size => new
                    {
                        variation_option_id = 0,
                        variation_option_name = size,
                        image_id = ""
                    }).ToArray()
                }
            };

            // Construir modelos (SKUs com preço e estoque)
            var models = productsSkuSellers.Select((sku, index) => new
            {
                tier_index = new[] { colors.IndexOf(sku.Color), sizes.IndexOf(sku.Size) },
                model_sku = sku.Sku,
                original_price = sku.Price,
                seller_stock = new[]
                {
                    new
                    {
                        stock = sku.Quantity
                    }
                }
            }).ToArray();

            logger.LogDebug("Initializing tier variations - ItemId: {ItemId}, ColorCount: {ColorCount}, SizeCount: {SizeCount}",
                itemId, colors.Count, sizes.Count);
            
            var responseInitSkus = await shopeeApiService.InitTierVariationAsync(productSeller.StoreId, itemId, standardiseTierVariation, models);
            
            logger.LogDebug("Initializing tier variations - ItemId: {ItemId}, ColorCount: {ColorCount}, SizeCount: {SizeCount}",
                itemId, colors.Count, sizes.Count);
            
            // Processar resposta e atualizar model_id dos SKUs
            if (responseInitSkus.RootElement.TryGetProperty("response", out var responseElement))
            {
                if (responseElement.TryGetProperty("model", out var modelsElement))
                {
                    var modelsArray = modelsElement.EnumerateArray().ToList();
                    
                    logger.LogDebug("Processing {Count} models from Shopee response", modelsArray.Count);

                    // Mapear model_sku para model_id a partir da resposta
                    var modelIdMap = new Dictionary<string, long>();
                    foreach (var model in modelsArray)
                    {
                        if (model.TryGetProperty("model_sku", out var skuElement) &&
                            model.TryGetProperty("model_id", out var modelIdElement))
                        {
                            var sku = skuElement.GetString();
                            var modelId = modelIdElement.GetInt64();
                            modelIdMap[sku] = modelId;

                            logger.LogDebug("Mapped SKU to ModelId - SKU: {Sku}, ModelId: {ModelId}", sku, modelId);
                        }
                    }

                    // Atualizar cada ProductSkuSeller com seu model_id
                    foreach (var productSkuSeller in productsSkuSellers)
                    {
                        if (modelIdMap.TryGetValue(productSkuSeller.Sku, out var modelId))
                        {
                            productSkuSeller.MarketplaceModelId = modelId.ToString();
                            productSkuSeller.MarketplaceProductId = itemId.ToString();
                            
                            await productSellerRepository.UpdateMarketplaceModelIdAsync(
                                productSeller.ProductId,
                                productSkuSeller.Sku,
                                productSeller.SellerId,
                                "shopee",
                                modelId.ToString(),
                                itemId.ToString()
                            );

                            logger.LogInformation("Updated SKU with ModelId - SKU: {Sku}, ModelId: {ModelId}, ProductId: {ProductId}",
                                productSkuSeller.Sku, modelId, productSeller.ProductId);
                        }
                        else
                        {
                            logger.LogWarning("SKU not found in Shopee response - SKU: {Sku}, ProductId: {ProductId}",
                                productSkuSeller.Sku, productSeller.ProductId);
                        }
                    }
                }
            }

            logger.LogInformation("Product uploaded with variations - ItemId: {ItemId}, ProductId: {ProductId}, SkuCount: {SkuCount}",
                itemId, productSeller.ProductId, productsSkuSellers.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uploading product - ProductId: {ProductId}, SellerId: {SellerId}",
                productSeller.ProductId, productSeller.SellerId);
            throw;
        }
    }

    public async Task UpdatePrice(List<ProductSkuSellerDomain> productsSkuSeller, decimal newPrice)
    {
        var productSkuSeller = productsSkuSeller.FirstOrDefault();
        
        var seller = await sellerRepository.GetSellerByIdAsync(productSkuSeller.SellerId);
        
        await shopeeApiService.UpdatePriceAsync(seller.ShopId, long.Parse(productSkuSeller.MarketplaceProductId), 
            productsSkuSeller.Select( x => 
            new PriceListDto
            {
                ModelId = long.Parse(x.MarketplaceModelId),
                OriginalPrice = newPrice
            }).ToList()
        );
    }
    
    public async Task UpdatePrice(ProductSkuSellerDomain productSkuSeller, decimal newPrice)
    {
        var seller = await sellerRepository.GetSellerByIdAsync(productSkuSeller.SellerId);
        
        await shopeeApiService.UpdatePriceAsync(seller.ShopId, long.Parse(productSkuSeller.MarketplaceProductId), [
            new PriceListDto()
            {
                ModelId = long.Parse(productSkuSeller.MarketplaceModelId),
                OriginalPrice = newPrice
            }
        ]);
    }
}
