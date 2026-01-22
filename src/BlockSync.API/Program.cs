using BlockSync.Application.Interfaces;
using BlockSync.Application.Services;
using BlockSync.Domain.Interfaces;
using BlockSync.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ========== SERVICIOS ==========

// Configurar logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Agregar controladores
builder.Services.AddControllers();

// Configurar Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "BlockSync.NET API",
        Version = "v1.0.0",
        Description = "Motor de sincronización de datos con integridad basada en blockchain. " +
                      "Sincronización inteligente usando comparación de hashes inspirada en Merkle Trees.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "BlockSync.NET"
        }
    });

    // Incluir comentarios XML si existen
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// ========== INYECCIÓN DE DEPENDENCIAS ==========

// Repositorios (Singleton para mantener datos en memoria durante la sesión)
builder.Services.AddSingleton<ISyncSource, LegacyRepository>();
builder.Services.AddSingleton<ISyncDestination, LocalRepository>();

// Servicios de aplicación
builder.Services.AddScoped<ISyncEngine, SyncEngine>();

// Configurar CORS si es necesario
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ========== CONSTRUCCIÓN DE LA APLICACIÓN ==========

var app = builder.Build();

// ========== CONFIGURACIÓN DEL PIPELINE HTTP ==========

// Habilitar Swagger en todos los entornos (incluso producción para esta PoC)
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "BlockSync.NET API v1");
    options.RoutePrefix = string.Empty; // Servir Swagger en la raíz (http://localhost:5000)
    options.DocumentTitle = "BlockSync.NET - API Documentation";
});

// Middleware
app.UseCors();
app.UseAuthorization();
app.MapControllers();

// ========== INFORMACIÓN DE INICIO ==========

app.Lifetime.ApplicationStarted.Register(() =>
{
    Console.WriteLine();
    Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║                    BLOCKSYNC.NET v1.0.0                    ║");
    Console.WriteLine("║          Motor de Sincronización Blockchain-Inspired       ║");
    Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
    Console.WriteLine();
    Console.WriteLine("🌐 Servidor iniciado en: http://localhost:5000");
    Console.WriteLine("📚 Swagger UI disponible en: http://localhost:5000");
    Console.WriteLine();
    Console.WriteLine("📡 Endpoints disponibles:");
    Console.WriteLine("   GET  /api/sync/status        - Estado del sistema");
    Console.WriteLine("   GET  /api/sync/diagnostics   - 🔬 Diagnóstico completo (demuestra 1M de registros)");
    Console.WriteLine("   POST /api/sync               - Ejecutar sincronización");
    Console.WriteLine("   POST /api/sync/hack/{y}/{m}  - Simular corrupción");
    Console.WriteLine("   POST /api/sync/reset         - Reiniciar sistema");
    Console.WriteLine("   GET  /api/sync/hashes        - Ver comparación de hashes");
    Console.WriteLine();
    Console.WriteLine("✨ Sistema listo para sincronizar datos!");
    Console.WriteLine("════════════════════════════════════════════════════════════");
    Console.WriteLine();
});

// Configurar URL
app.Urls.Clear();
app.Urls.Add("http://localhost:5000");

app.Run();
