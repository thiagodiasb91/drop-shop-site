using Microsoft.AspNetCore.Mvc;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Dropship.Configuration;
using Dropship.Domain;
using Dropship.Repository;
using Dropship.Requests;
using Microsoft.AspNetCore.Authorization;

namespace Dropship.Controllers;

[ApiController]
[Route("users")]
public class UserController : ControllerBase
{
    private readonly IAmazonCognitoIdentityProvider _cognitoClient;
    private readonly UserRepository _userRepository;
    private readonly ILogger<UserController> _logger;

    public UserController(IAmazonCognitoIdentityProvider cognitoClient, UserRepository userRepository, ILogger<UserController> logger)
    {
        _cognitoClient = cognitoClient;
        _userRepository = userRepository;
        _logger = logger;
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        _logger.LogInformation("Updating user with ID: {Id}", id);

        try
        {
            var listRequest = new ListUsersRequest { UserPoolId = AuthConfig.USER_POOL_ID };
            var response = await _cognitoClient.ListUsersAsync(listRequest);
            
            if (id < 1 || id > response.Users.Count)
            {
                _logger.LogWarning("User ID {Id} not found", id);
                return NotFound(new { error = "User not found" });
            }

            var cognitoUser = response.Users[id - 1];
            var email = cognitoUser.Attributes.FirstOrDefault(a => a.Name == "email")?.Value ?? "";
            
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Email not found for user ID: {Id}", id);
                return BadRequest(new { error = "User email not found" });
            }

            var existingUser = await _userRepository.GetUser(email);
            
            if (existingUser == null)
            {
                var newUser = new UserDomain
                {
                    Id = id.ToString(),
                    Email = email,
                    Role = request.Role
                };
                
                await _userRepository.CreateUserAsync(newUser);
                _logger.LogInformation("Created new user in DynamoDB for email: {Email}", email);
            }
            else
            {
                if (!string.IsNullOrEmpty(request.Role))
                    existingUser.Role = request.Role;
                    
                await _userRepository.UpdateUserAsync(existingUser);
                _logger.LogInformation("Updated user in DynamoDB for email: {Email}", email);
            }

            return Ok(new { ok = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user with ID: {Id}", id);
            return BadRequest(new { error = "Failed to update user" });
        }
    }
    
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        _logger.LogInformation("Fetching all users from Cognito");

        try
        {
            var request = new ListUsersRequest
            {
                UserPoolId = AuthConfig.USER_POOL_ID
            };

            var response = await _cognitoClient.ListUsersAsync(request);
            
            var users = new List<object>();
            
            for (int i = 0; i < response.Users.Count; i++)
            {
                var user = response.Users[i];
                var email = user.Attributes.FirstOrDefault(a => a.Name == "email")?.Value ?? "";
                
                var dbUser = await _userRepository.GetUser(email);
                var role = dbUser?.Role ?? null;
                
                users.Add(new
                {
                    id = i + 1,
                    name = user.Attributes.FirstOrDefault(a => a.Name == "name")?.Value ?? "",
                    cognitoId = user.Username,
                    email = email,
                    emailVerified = user.Attributes.FirstOrDefault(a => a.Name == "email_verified")?.Value == "true",
                    role = role,
                    saving = false
                });
            }

            _logger.LogInformation("Retrieved {Count} users from Cognito", users.Count);
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching users from Cognito");
            return BadRequest(new { error = "Failed to fetch users" });
        }
    }
}