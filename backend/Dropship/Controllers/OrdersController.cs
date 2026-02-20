using Microsoft.AspNetCore.Mvc;

namespace Dropship.Controllers;

[ApiController]
[Route("orders")]
public class OrdersController : ControllerBase
{
    [HttpPost("{orderSn}/shops/{shopId}/process")]
    public async Task<IActionResult> ProcessOrder()
    {
        // Lógica para processar o pedido
        return Ok(new { message = "Pedido processado com sucesso" });
    }
}