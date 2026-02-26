using System.ComponentModel.DataAnnotations;

namespace Dropship.Requests;

/// <summary>
/// Request para criar um novo fornecedor (supplier)
/// </summary>
public class CreateSupplierRequest
{
    /// <summary>
    /// Nome do fornecedor
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 200 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Razão social
    /// </summary>
    [Required(ErrorMessage = "Legal name is required")]
    [StringLength(300, MinimumLength = 2, ErrorMessage = "Legal name must be between 2 and 300 characters")]
    public string LegalName { get; set; } = string.Empty;

    /// <summary>
    /// Telefone
    /// </summary>
    [Phone(ErrorMessage = "Phone must be a valid phone number")]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Rua/logradouro
    /// </summary>
    [StringLength(200)]
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Número do endereço
    /// </summary>
    [StringLength(20)]
    public string AddressNumber { get; set; } = string.Empty;

    /// <summary>
    /// Bairro
    /// </summary>
    [StringLength(100)]
    public string AddressDistrict { get; set; } = string.Empty;

    /// <summary>
    /// Cidade
    /// </summary>
    [StringLength(100)]
    public string AddressCity { get; set; } = string.Empty;

    /// <summary>
    /// Estado (sigla: SP, RJ, etc)
    /// </summary>
    [StringLength(2, MinimumLength = 2, ErrorMessage = "State must be a 2-character abbreviation")]
    public string AddressState { get; set; } = string.Empty;

    /// <summary>
    /// CEP (8 dígitos)
    /// </summary>
    [RegularExpression(@"^\d{8}$", ErrorMessage = "Zipcode must be 8 digits")]
    public string AddressZipcode { get; set; } = string.Empty;
    
    /// <summary>
    /// CNPJ (14 dígitos ou formato XX.XXX.XXX/XXXX-XX)
    /// </summary>
    [Required(ErrorMessage = "CNPJ is required")]
    [CustomValidation(typeof(CreateSupplierRequest), nameof(ValidateCnpj))]
    public string Cnpj { get; set; } = string.Empty;

    /// <summary>
    /// CST/CSOSN (classificação fiscal)
    /// </summary>
    [RegularExpression(@"^\d{3}$", ErrorMessage = "CST/CSOSN must be 3 digits")]
    public string CstCsosn { get; set; } = string.Empty;
    
    /// <summary>
    /// ID no sistema eNota (opcional)
    /// </summary>
    public string? EnotasId { get; set; }

    public string InfinityPayHandle { get; set; }

    /// <summary>
    /// Validação customizada para CNPJ
    /// </summary>
    public static ValidationResult? ValidateCnpj(string cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj))
            return new ValidationResult("CNPJ is required");

        if (!Validators.CnpjValidator.IsValid(cnpj))
            return new ValidationResult("CNPJ must be valid (14 digits with correct verifiers)");

        return ValidationResult.Success;
    }
}
