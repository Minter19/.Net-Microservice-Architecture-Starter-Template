using Microsoft.EntityFrameworkCore;
using Catalog.API.Endpoints;
using Catalog.API.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
//kita ambil dulu connection string dari appsettings.json
var connectionString = builder.Configuration.GetConnectionString("CatalogDbConnection") 
    ?? "Data Source=catalog.db";
//kita daftarkan CatalogDbContext sebagai service
builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseSqlServer(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseHttpsRedirection();

try
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

    // Menunggu dan mencoba terhubung ke DB (penting untuk Docker)
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Attempting to connect to database...");

    // Logika retry sederhana jika DB container belum siap
    var retries = 5;
    while (retries > 0)
    {
        try
        {
            dbContext.Database.Migrate();
            logger.LogInformation("Database migration successful.");
            break;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database connection failed. Retrying... ({retries} left)", retries);
            retries--;
            await Task.Delay(5000);
        }
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred while migrating the database.");
}

app.MapProductEndpoints();

app.Run();
