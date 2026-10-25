
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient();

var app = builder.Build();

app.MapControllers();

// Print clickable API URLs after the server starts
app.Lifetime.ApplicationStarted.Register(() =>
{
    var server = app.Services.GetRequiredService<IServer>();

    var addresses = server.Features
        .Get<IServerAddressesFeature>()?.Addresses;

    if (addresses == null || addresses.Count == 0)
    {
        Console.WriteLine("Server address not found.");
        return;
    }

    var baseUrl = addresses.FirstOrDefault(
        url => url.StartsWith(
            "http://",
            StringComparison.OrdinalIgnoreCase));

    baseUrl ??= addresses.First();

    baseUrl = baseUrl.TrimEnd('/');

    var jsonUrl =
        $"{baseUrl}/api/QueryData/GetPointShot2GisUrl?x=24.7976198&y=46.6842152&z=17&w=1000&h=700";

    var imageUrl =
        $"{baseUrl}/api/QueryData/GetPointShot?x=24.7976198&y=46.6842152&z=17&w=1000&h=700";

    Console.WriteLine();
    Console.WriteLine("========== GIS API LINKS ==========");

    Console.WriteLine("JSON API:");
    Console.WriteLine(jsonUrl);

    Console.WriteLine();
    Console.WriteLine("Map Image:");
    Console.WriteLine(imageUrl);

    Console.WriteLine("===================================");
});

app.Run();
