using BlockSync.Application.Interfaces;
using BlockSync.Application.Services;
using BlockSync.Application.Validation;
using BlockSync.Domain.Configuration;
using BlockSync.Domain.Entities;
using BlockSync.Domain.Interfaces;
using BlockSync.Infrastructure.Repositories;
using BlockSync.Infrastructure.Services;

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

// Cargar y validar configuración de mapeo de base de datos
var databaseMapping = builder.Configuration.GetSection("DatabaseMapping").Get<DatabaseMappingConfiguration>();
if (databaseMapping != null)
{
    var validator = new MappingConfigValidator();
    var validationResult = validator.Validate(databaseMapping);

    if (!validationResult.IsValid)
    {
        Console.WriteLine("❌ Error en configuración de DatabaseMapping:");
        foreach (var error in validationResult.Errors)
        {
            Console.WriteLine($"   - {error}");
        }
        throw new InvalidOperationException("La configuración de DatabaseMapping contiene errores");
    }

    // Registrar configuración como Singleton
    builder.Services.AddSingleton(databaseMapping);
    Console.WriteLine("✅ DatabaseMapping configurado y validado correctamente");
}

// Repositorios SQLite (Scoped para conexiones por request)
// SEPARADOS: source.db para origen, destination.db para destino
builder.Services.AddScoped<ISyncSource, SqliteSourceRepository>();
builder.Services.AddScoped<ISyncDestination, SqliteDestinationRepository>();

// ========== REPOSITORIO GENÉRICO (Alternativa flexible) ==========
// NOTA: Para usar GenericSyncRepository en lugar de los repositorios específicos,
// descomentar las siguientes líneas y comentar las anteriores:
//
// builder.Services.AddScoped<ISyncSource>(sp =>
// {
//     var config = sp.GetRequiredService<DatabaseMappingConfiguration>();
//     var logger = sp.GetRequiredService<ILogger<GenericSyncRepository<Venta>>>();
//     return new GenericSyncRepository<Venta>(
//         config.Source,
//         "Ventas",
//         config.DataTransformations,
//         logger,
//         batchSize: 5000
//     );
// });
//
// builder.Services.AddScoped<ISyncDestination>(sp =>
// {
//     var config = sp.GetRequiredService<DatabaseMappingConfiguration>();
//     var logger = sp.GetRequiredService<ILogger<GenericSyncRepository<Venta>>>();
//     return new GenericSyncRepository<Venta>(
//         config.Destination,
//         "Ventas",
//         config.DataTransformations,
//         logger,
//         batchSize: 5000
//     );
// });

// Seeder de datos (Scoped para permitir inicialización bajo demanda)
builder.Services.AddScoped<SqliteDataSeeder>();

// Motor de sincronización
builder.Services.AddScoped<ISyncEngine, SyncEngine>();

// ========== REPOSITORIOS IN-MEMORY (PoC original - comentados) ==========
// builder.Services.AddSingleton<ISyncSource, LegacyRepository>();
// builder.Services.AddSingleton<ISyncDestination, LocalRepository>();

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
    Console.WriteLine("║                     SQLite Local Edition                   ║");
    Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
    Console.WriteLine();
    Console.WriteLine("🌐 Servidor iniciado en: http://localhost:5000");
    Console.WriteLine("📚 Swagger UI disponible en: http://localhost:5000");
    Console.WriteLine("💾 Base de datos: SQLite (./data/blocksync.db)");
    Console.WriteLine();
    Console.WriteLine("📡 Endpoints disponibles:");
    Console.WriteLine("   GET  /api/sync/status        - Estado del sistema");
    Console.WriteLine("   GET  /api/sync/diagnostics   - 🔬 Diagnóstico completo (demuestra 1M de registros)");
    Console.WriteLine("   POST /api/sync               - Ejecutar sincronización");
    Console.WriteLine("   POST /api/sync/hack/{y}/{m}  - Simular corrupción");
    Console.WriteLine("   POST /api/sync/reset         - Reiniciar sistema (regenera 1M registros)");
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
