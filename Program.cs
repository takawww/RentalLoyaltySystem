
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;

using RentalLoyaltySystem;
using RentalLoyaltySystem.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
var config = builder.Configuration;

builder.RootComponents.Add<App>("#app");
builder.Services.AddScoped<AuthenticationStateProvider, AuthService>();
builder.Services.AddScoped<AzureStorageRepository>();
builder.Services.AddAuthorizationCore();

await builder.Build().RunAsync();
