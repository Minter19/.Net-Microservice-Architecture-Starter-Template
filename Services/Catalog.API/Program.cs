// Impor namespace yang diperlukan.
using Microsoft.EntityFrameworkCore;
using Catalog.API.Endpoints; // Ini adalah file custom Anda untuk endpoint
using Catalog.API.Data;     // Ini adalah DbContext Anda
using Microsoft.AspNetCore.Builder;

// 1. ========================================================================
// Inisialisasi Aplikasi (Bagian 'Services' / Dependency Injection)
// ========================================================================

// Membuat 'builder' WebApplication. Ini adalah titik awal yang 
// mengkonfigurasi semua layanan (services) dan logging.
var builder = WebApplication.CreateBuilder(args);

// --- Konfigurasi Swagger/OpenAPI ---
// Menambahkan layanan 'ApiExplorer' yang penting.
// Ini diperlukan agar Minimal API (seperti MapGet, MapPost) 
// bisa ditemukan oleh Swagger.
builder.Services.AddEndpointsApiExplorer();

// Menambahkan layanan generator Swagger (Swashbuckle).
// Ini adalah layanan yang akan membuat file 'swagger.json'.
builder.Services.AddSwaggerGen(options =>
{
    // Mengkonfigurasi dokumen OpenAPI (Swagger)
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Catalog API",
        Version = "v1"
    });
});
// --- Akhir Konfigurasi Swagger ---

// Mengambil Connection String dari 'appsettings.json'.
var connectionString = builder.Configuration.GetConnectionString("CatalogDbConnection")
    // Jika tidak ditemukan (misal, dalam mode development), gunakan database lokal.
    ?? "Data Source=catalog.db";

// Mendaftarkan CatalogDbContext ke dalam Dependency Injection (DI) container.
// Ini memberitahu aplikasi cara membuat DbContext:
builder.Services.AddDbContext<CatalogDbContext>(options =>
    // ...yaitu dengan menggunakan SQL Server dan connection string yang tadi.
    options.UseSqlServer(connectionString));

// Mendaftarkan layanan Health Check. 
// Ini hanya mendaftarkan service-nya, belum membuat endpoint-nya.
builder.Services.AddHealthChecks();

// 2. ========================================================================
// Pembangunan Aplikasi (Bagian 'Pipeline' / Middleware)
// ========================================================================

// 'app.Build()' adalah titik pemisah. 
// Semua di atas 'builder' adalah konfigurasi 'services'.
// Semua di bawah 'app' adalah konfigurasi 'middleware pipeline' (urutan eksekusi HTTP).
var app = builder.Build();

// --- Konfigurasi Middleware Swagger ---
// Middleware ini bertugas MENGHASILKAN file 'swagger.json'
// berdasarkan konfigurasi dari AddSwaggerGen di atas.
app.UseSwagger();

// Middleware ini bertugas MENAMPILKAN UI (HTML/JS/CSS) Swagger yang interaktif.
app.UseSwaggerUI(options =>
{
    // Memberitahu UI di mana menemukan file JSON yang tadi dibuat.
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog API v1");
    // Mengatur URL agar UI Swagger bisa diakses di '/swagger' 
    // (misal: https://localhost:xxxx/swagger)
    options.RoutePrefix = "swagger";
});
// --- Akhir Middleware Swagger ---

// Mengaktifkan endpoint untuk Health Check di URL '/health'.
// Saat Anda mengakses /health, ini akan menjalankan layanan yang didaftarkan tadi.
app.UseHealthChecks("/health");

// Membuat endpoint sederhana di root ('/') untuk
// memastikan service berjalan.
app.MapGet("/", () =>
{
    return "Catalog Service is Running!";
});

// 3. ========================================================================
// Migrasi Database Otomatis (Best Practice untuk Startup)
// ========================================================================
// Blok 'try-catch' ini untuk menjalankan migrasi database EF Core secara
// otomatis saat aplikasi pertama kali berjalan.
try
{
    // Best Practice: DbContext adalah 'scoped' service. 
    // Kita tidak boleh me-resolve-nya langsung dari 'app.Services' (singleton).
    // Kita harus membuat 'scope' baru untuk mengambil service yang 'scoped'.
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Attempting to connect to database...");

    // Logika Retry: Ini sangat penting, terutama di lingkungan Docker/Kubernetes.
    // Web app mungkin saja menyala lebih cepat daripada container database.
    // Kita beri 5 kali percobaan (total ~25 detik) untuk database agar siap.
    var retries = 5;
    while (retries > 0)
    {
        try
        {
            // Menjalankan migrasi database yang tertunda.
            dbContext.Database.Migrate();
            logger.LogInformation("Database migration successful.");
            break; // Berhasil, keluar dari loop
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database connection failed. Retrying... ({retries} left)", retries);
            retries--;
            await Task.Delay(5000); // Tunggu 5 detik sebelum mencoba lagi
        }
    }
}
catch (Exception ex)
{
    // Jika migrasi gagal total, catat error-nya.
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred while migrating the database.");
}

// 4. ========================================================================
// Pendaftaran Endpoint dan Menjalankan Aplikasi
// ========================================================================

// Ini adalah pola 'Endpoint Grouping' (Clean Code).
// 'MapProductEndpoints' adalah extension method yang Anda buat sendiri 
// (di file Catalog.API.Endpoints) yang berisi semua MapGet, MapPost, dll.
// untuk 'Product'.
app.MapProductEndpoints();

// Menjalankan aplikasi dan mulai mendengarkan request HTTP.
app.Run();