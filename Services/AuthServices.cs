using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Authorization;

namespace RentalLoyaltySystem.Services;

public class AuthService(
    JSRuntime js,
    JwtSecurityTokenHandler tokenHandler) : AuthenticationStateProvider
{
    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await js.InvokeAsync<string>("localStorage.getItem", "authToken");

        if (string.IsNullOrWhiteSpace(token))
            return new AuthenticationState(_currentUser);

        var jwtToken = tokenHandler.ReadJwtToken(token);
        if (jwtToken.ValidTo < DateTime.UtcNow)
        {
            // Token expired, clear it
            await js.InvokeVoidAsync("localStorage.removeItem", "authToken");
            return new AuthenticationState(_currentUser);
        }

        var identity = new ClaimsIdentity(jwtToken.Claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        return new AuthenticationState(user);
    }

    public async Task MarkUserAsAuthenticatedAsync(string token)
    {
        await js.InvokeVoidAsync("localStorage.setItem", "authToken", token);

        var jwtToken = tokenHandler.ReadJwtToken(token);
        var identity = new ClaimsIdentity(jwtToken.Claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

     public async Task MarkUserAsLoggedOutAsync()
    {
        await js.InvokeVoidAsync("localStorage.removeItem", "authToken");
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        // Example only – replace with real backend check
        if (username == "admin" && password == "1234")
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, username)
            }, "FakeAuth");

            _currentUser = new ClaimsPrincipal(identity);
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
            return true;
        }

        return false;
    }

    public async Task Logout()
    {
        await MarkUserAsLoggedOutAsync();
    }
}
