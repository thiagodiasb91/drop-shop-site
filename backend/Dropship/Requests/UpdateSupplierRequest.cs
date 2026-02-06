using System.ComponentModel.DataAnnotations;

namespace Dropship.Requests;

/// <summary>
/// Request para atualizar um fornecedor (supplier)
/// </summary>
public class UpdateSupplierRequest
{
    /// <summary>
    /// Nome do fornecedor (opcional)
    /// </summary>
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 200 characters")]
    public string? Name { get; set; }

    /// <summary>
    /// Razão social (opcional)
    /// </summary>
    [StringLength(300, MinimumLength = 2, ErrorMessage = "Legal name must be between 2 and 300 characters")]
    public string? LegalName { get; set; }

    /// <summary>
    /// Telefone (opcional)
    /// </summary>
    [Phone(ErrorMessage = "Phone must be a valid phone number")]
    [StringLength(20)]
    public string? Phone { get; set; }

    /// <summary>
    /// Prioridade (opcional)
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Priority must be a non-negative number")]
    public int? Priority { get; set; }
    
    /// <summary>
    /// Rua/logradouro (opcional)
    /// </summary>
    [StringLength(200)]
    public string? Address { get; set; }

    /// <summary>
    /// Número do endereço (opcional)
    /// </summary>
    [StringLength(20)]
    public string? AddressNumber { get; set; }

    /// <summary>
    /// Bairro (opcional)
    /// </summary>
    [StringLength(100)]
    public string? AddressDistrict { get; set; }

    /// <summary>
    /// Cidade (opcional)
    /// </summary>
    [StringLength(100)]
    public string? AddressCity { get; set; }

    /// <summary>
    /// Estado (sigla: SP, RJ, etc) (opcional)
    /// </summary>
    [StringLength(2, MinimumLength = 2, ErrorMessage = "State must be a 2-character abbreviation")]
    public string? AddressState { get; set; }

    /// <summary>
    /// CEP (8 dígitos) (opcional)
    /// </summary>
    [RegularExpression(@"^\d{8}$", ErrorMessage = "Zipcode must be 8 digits")]
    public string? AddressZipcode { get; set; }
    
    /// <summary>
    /// CNPJ (14 dígitos ou formato XX.XXX.XXX/XXXX-XX) (opcional)
    /// </summary>
    [CustomValidation(typeof(UpdateSupplierRequest), nameof(ValidateCnpj))]
    public string? Cnpj { get; set; }

    /// <summary>
    /// CST/CSOSN (classificação fiscal) (opcional)
    /// </summary>
    [RegularExpression(@"^\d{3}$", ErrorMessage = "CST/CSOSN must be 3 digits")]
    public string? CstCsosn { get; set; }
    
    /// <summary>
    /// ID no sistema eNota (opcional)
    /// </summary>
    public string? EnotasId { get; set; }

    /// <summary>
    /// Validação customizada para CNPJ (opcional)
    /// </summary>
    public static ValidationResult? ValidateCnpj(string? cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj))
            return ValidationResult.Success; // Opcional

        if (!Validators.CnpjValidator.IsValid(cnpj))
            return new ValidationResult("CNPJ must be valid (14 digits with correct verifiers)");

        return ValidationResult.Success;
    }
}
