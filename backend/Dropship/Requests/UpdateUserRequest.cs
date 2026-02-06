namespace Dropship.Requests;

public record UpdateUserRequest(string? Name, string? Email, string? Role);