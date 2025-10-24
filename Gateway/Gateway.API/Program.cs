var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<AppInstanceId>();

//add reverse proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        // Konfigurasi ini mengambil file JSON yang sudah kita proxy
        // Ini adalah kunci untuk menggabungkan Swagger
        options.SwaggerEndpoint(
            "/swagger/catalog/v1/swagger.json", // Path di Gateway
            "Catalog.API v1" // Nama yang tampil di dropdown Swagger
        );
        //
        // Nanti, jika Anda punya Ordering.API, tambahkan di sini:
        // options.SwaggerEndpoint(
        //     "/swagger/ordering/v1/swagger.json", 
        //     "Ordering.API v1"
        // );
    });
}

//app.UseHttpsRedirection();

app.MapGet("/", () => "Welcome to the Gateway API! with Instance ID: " + app.Services.GetRequiredService<AppInstanceId>().InstanceId);

app.MapReverseProxy();

app.Run();

//create instance of web application
internal sealed class AppInstanceId
{
    public Guid InstanceId { get; } = Guid.NewGuid();
}