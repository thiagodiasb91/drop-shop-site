namespace Dropship.Validators;

/// <summary>
/// Validador para CNPJ brasileiro
/// </summary>
public static class CnpjValidator
{
    /// <summary>
    /// Valida se o CNPJ é válido
    /// Verifica:
    /// - Formato (14 dígitos)
    /// - Dígitos verificadores
    /// </summary>
    /// <param name="cnpj">CNPJ com ou sem formatação</param>
    /// <returns>True se CNPJ é válido, false caso contrário</returns>
    public static bool IsValid(string? cnpj)
    {
        int[] multiplicador1 = new int[12] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] multiplicador2 = new int[13] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
    
        // Remove caracteres especiais
        cnpj = cnpj.Trim().Replace(".", "").Replace("-", "").Replace("/", "");
        if (cnpj.Length != 14) return false;

        string tempCnpj = cnpj.Substring(0, 12);
        int soma = 0;

        // Cálculo do 1º dígito
        for (int i = 0; i < 12; i++)
            soma += int.Parse(tempCnpj[i].ToString()) * multiplicador1[i];
    
        int resto = (soma % 11);
        if (resto < 2) resto = 0;
        else resto = 11 - resto;
    
        string digito = resto.ToString();
        tempCnpj = tempCnpj + digito;
    
        // Cálculo do 2º dígito
        soma = 0;
        for (int i = 0; i < 13; i++)
            soma += int.Parse(tempCnpj[i].ToString()) * multiplicador2[i];
    
        resto = (soma % 11);
        if (resto < 2) resto = 0;
        else resto = 11 - resto;
    
        digito = digito + resto.ToString();
    
        return cnpj.EndsWith(digito);
    }
}
