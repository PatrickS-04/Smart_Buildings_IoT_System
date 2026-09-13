using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PROG7312_POE;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseAddress = builder.HostEnvironment.IsDevelopment()
    ? "https://localhost:7194/"
    : builder.HostEnvironment.BaseAddress;

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseAddress) });

await builder.Build().RunAsync();
