using BasicHud;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SOU.Hud.Sdk.Blazor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSouHud<HudOptions>(HudOptionsJson.Default.HudOptions);
await builder.Build().RunAsync();
