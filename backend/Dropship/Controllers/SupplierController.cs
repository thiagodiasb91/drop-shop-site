using Microsoft.AspNetCore.Mvc;
using Dropship.Repository;
using Dropship.Requests;
using Dropship.Responses;

namespace Dropship.Controllers;

/// <summary>
/// Controller para gerenciar fornecedores (suppliers)
/// </summary>
[ApiController]
[Route("suppliers")]
public class SupplierController : ControllerBase
{
    private readonly SupplierRepository _supplierRepository;
    private readonly ILogger<SupplierController> _logger;

    public SupplierController(SupplierRepository supplierRepository, ILogger<SupplierController> logger)
    {
        _supplierRepository = supplierRepository;
        _logger = logger;
    }

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
        _logger.LogInformation("Fetching supplier with ID: {SupplierId}", id);

        try
        {
            var supplier = await _supplierRepository.GetSupplierAsync(id);
            if (supplier == null)
            {
                _logger.LogWarning("Supplier not found with ID: {SupplierId}", id);
                return NotFound(new { error = "Supplier not found" });
            }

            var response = supplier.ToResponse();
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching supplier with ID: {SupplierId}", id);
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
        _logger.LogInformation("Fetching all suppliers from database");

        try
        {
            var suppliers = await _supplierRepository.GetAllSuppliersAsync();
            var response = suppliers.ToListResponse();

            _logger.LogInformation("Retrieved {Count} suppliers", response.Total);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching suppliers");
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
        _logger.LogInformation("Creating supplier with name: {SupplierName}", request.Name);

        try
        {
            // Validações de Data Annotations são automáticas no ModelState
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid supplier request - ModelState errors");
                return BadRequest(ModelState);
            }

            var supplier = await _supplierRepository.CreateSupplierAsync(request);
            var response = supplier.ToResponse();

            _logger.LogInformation("Supplier created successfully with ID: {SupplierId}", supplier.Id);
            return CreatedAtAction(nameof(GetSupplier), new { id = supplier.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating supplier");
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
        _logger.LogInformation("Updating supplier with ID: {SupplierId}", id);

        try
        {
            // Validações de Data Annotations são automáticas no ModelState
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid supplier request - ModelState errors");
                return BadRequest(ModelState);
            }

            var supplier = await _supplierRepository.UpdateSupplierAsync(id, request);
            if (supplier == null)
            {
                _logger.LogWarning("Supplier not found with ID: {SupplierId}", id);
                return NotFound(new { error = "Supplier not found" });
            }

            var response = supplier.ToResponse();
            _logger.LogInformation("Supplier updated successfully with ID: {SupplierId}", id);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating supplier with ID: {SupplierId}", id);
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
        _logger.LogInformation("Deleting supplier with ID: {SupplierId}", id);

        try
        {
            var success = await _supplierRepository.DeleteSupplierAsync(id);
            if (!success)
            {
                _logger.LogWarning("Supplier not found with ID: {SupplierId}", id);
                return NotFound(new { error = "Supplier not found" });
            }

            _logger.LogInformation("Supplier deleted successfully with ID: {SupplierId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting supplier with ID: {SupplierId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
        }
    }
}
