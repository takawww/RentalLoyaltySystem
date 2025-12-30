
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;

using RentalLoyaltySystem;
using RentalLoyaltySystem.Services;




using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);




var builder = WebAssemblyHostBuilder.CreateDefault(args);
var config = builder.Configuration;

builder.RootComponents.Add<App>("#app");
builder.Services.AddScoped<AuthenticationStateProvider, AuthService>();


    
builder.Services.AddAuthorizationCore();

await builder.Build().RunAsync();
