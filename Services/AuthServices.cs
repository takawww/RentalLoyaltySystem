using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Azure.Data.Tables;

namespace RentalLoyaltySystem.Services;

public class AuthService : AuthenticationStateProvider
{
    private readonly IJSRuntime _js;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly AzureStorageRepository _storageRepository;

    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

    public AuthService(IJSRuntime js, AzureStorageRepository storageRepository)
    {
        _js = js;
        _tokenHandler = new JwtSecurityTokenHandler();
        _storageRepository = storageRepository;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _js.InvokeAsync<string>("localStorage.getItem", "authToken");

        if (string.IsNullOrWhiteSpace(token))
            return new AuthenticationState(_currentUser);

        var jwtToken = _tokenHandler.ReadJwtToken(token);
        if (jwtToken.ValidTo < DateTime.UtcNow)
        {
            // Token expired, clear it
            await _js.InvokeVoidAsync("localStorage.removeItem", "authToken");
            return new AuthenticationState(_currentUser);
        }

        var identity = new ClaimsIdentity(jwtToken.Claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        return new AuthenticationState(user);
    }

    public async Task MarkUserAsAuthenticatedAsync(string token)
    {
        await _js.InvokeVoidAsync("localStorage.setItem", "authToken", token);

        var jwtToken = _tokenHandler.ReadJwtToken(token);
        var identity = new ClaimsIdentity(jwtToken.Claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public async Task MarkUserAsLoggedOutAsync()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", "authToken");
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        try
        {
            // Query the users table by email (partitionKey) and password (rowKey)
            var user = await _storageRepository.GetAsync<UserEntity>("users", username, password);

            if (user != null)
            {
                // User found and password matches
                var identity = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, user.PartitionKey),
                    new Claim(ClaimTypes.Email, user.Email ?? "")
                }, "AzureAuth");

                _currentUser = new ClaimsPrincipal(identity);
                NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            // Log the exception if needed
            System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
            return false;
        }
    }

    public async Task Logout()
    {
        await MarkUserAsLoggedOutAsync();
    }
}

public class UserEntity : Azure.Data.Tables.ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public Azure.ETag ETag { get; set; }
    public string Password { get; set; } = string.Empty;
    public string? Email { get; set; }
}
