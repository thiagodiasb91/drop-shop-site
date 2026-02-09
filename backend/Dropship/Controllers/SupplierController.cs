using Microsoft.AspNetCore.Mvc;
using Dropship.Repository;
using Dropship.Requests;
using Dropship.Responses;
using Microsoft.AspNetCore.Authorization;

namespace Dropship.Controllers;

/// <summary>
/// Controller para gerenciar fornecedores (suppliers)
/// </summary>
[ApiController]
[Route("suppliers")]
[Authorize]
public class SupplierController(SupplierRepository supplierRepository, UserRepository userRepository, ILogger<SupplierController> logger)
    : ControllerBase
{
    /// <summary>
    /// Obtém informações de um fornecedor pelo ID
    /// </summary>
    /// <param name="id">ID do fornecedor</param>
    /// <returns>Informações do fornecedor</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SupplierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSupplier(string id)
    {
        logger.LogInformation("Fetching supplier with ID: {SupplierId}", id);

        try
        {
            var supplier = await supplierRepository.GetSupplierAsync(id);
            if (supplier == null)
            {
                logger.LogWarning("Supplier not found with ID: {SupplierId}", id);
                return NotFound(new { error = "Supplier not found" });
            }

            var response = supplier.ToResponse();
            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching supplier with ID: {SupplierId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Lista todos os fornecedores (usando GSI_RELATIONS_LOOKUP)
    /// </summary>
    /// <returns>Lista de fornecedores ordenados por prioridade</returns>
    [HttpGet]
    [ProducesResponseType(typeof(SupplierListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllSuppliers()
    {
        logger.LogInformation("Fetching all suppliers from database");

        try
        {
            var suppliers = await supplierRepository.GetAllSuppliersAsync();
            var response = suppliers.ToListResponse();

            logger.LogInformation("Retrieved {Count} suppliers", response.Total);
            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching suppliers");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Cria um novo fornecedor
    /// </summary>
    /// <param name="request">Dados do novo fornecedor</param>
    /// <returns>Fornecedor criado</returns>
    [HttpPost]
    [ProducesResponseType(typeof(SupplierResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierRequest request)
    {
        logger.LogInformation("Creating supplier with name: {SupplierName}", request.Name);

        try
        {
            // Validações de Data Annotations são automáticas no ModelState
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Invalid supplier request - ModelState errors");
                return BadRequest(ModelState);
            }
    
            var supplier = await supplierRepository.CreateSupplierAsync(request);
            var response = supplier.ToResponse();

            var userEmail = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "email")?.Value;

            await userRepository.SetResourceId(userEmail, supplier.Id);
            logger.LogInformation("Supplier created successfully with ID: {SupplierId}", supplier.Id);
            return CreatedAtAction(nameof(GetSupplier), new { id = supplier.Id }, response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating supplier");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Atualiza um fornecedor existente
    /// </summary>
    /// <param name="id">ID do fornecedor</param>
    /// <param name="request">Dados a atualizar</param>
    /// <returns>Fornecedor atualizado</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(SupplierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSupplier(string id, [FromBody] UpdateSupplierRequest request)
    {
        logger.LogInformation("Updating supplier with ID: {SupplierId}", id);

        try
        {
            // Validações de Data Annotations são automáticas no ModelState
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Invalid supplier request - ModelState errors");
                return BadRequest(ModelState);
            }

            var supplier = await supplierRepository.UpdateSupplierAsync(id, request);
            if (supplier == null)
            {
                logger.LogWarning("Supplier not found with ID: {SupplierId}", id);
                return NotFound(new { error = "Supplier not found" });
            }

            var response = supplier.ToResponse();
            logger.LogInformation("Supplier updated successfully with ID: {SupplierId}", id);
            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating supplier with ID: {SupplierId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Deleta um fornecedor
    /// </summary>
    /// <param name="id">ID do fornecedor</param>
    /// <returns>Resultado da operação</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSupplier(string id)
    {
        logger.LogInformation("Deleting supplier with ID: {SupplierId}", id);

        try
        {
            var success = await supplierRepository.DeleteSupplierAsync(id);
            if (!success)
            {
                logger.LogWarning("Supplier not found with ID: {SupplierId}", id);
                return NotFound(new { error = "Supplier not found" });
            }

            logger.LogInformation("Supplier deleted successfully with ID: {SupplierId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting supplier with ID: {SupplierId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }
}
