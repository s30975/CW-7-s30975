var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

app.UseRouting();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/", () => "Travel Agency API is running. Use /api/trips to access trips data.");

try
{
    Console.WriteLine($"Application starting on port {app.Urls.FirstOrDefault()}");
    await app.RunAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"Application startup failed: {ex.Message}");
}

