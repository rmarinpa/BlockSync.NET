using System.IO.Compression;
using System.Text.Json;
using BlockSync.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace BlockSync.Infrastructure.Services;

/// <summary>
/// Cliente HTTP para consumir datos de ChileCompra (Mercado Público).
/// Descarga archivos JSONL comprimidos con datos OCDS y parsea awards.
/// </summary>
public class ChileCompraApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ChileCompraApiClient> _logger;

    // URL base para descargas masivas OCDS desde Open Contracting Partnership
    private const string BaseDownloadUrl = "https://data.open-contracting.org/en/publication/144/download";

    public ChileCompraApiClient(
        HttpClient httpClient,
        ILogger<ChileCompraApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Configurar timeout generoso para descargas grandes
        _httpClient.Timeout = TimeSpan.FromMinutes(30);
    }

    /// <summary>
    /// Descarga archivo JSONL.gz de un año específico y extrae los awards.
    /// </summary>
    /// <param name="year">Año a descargar (ej: 2025, 2024)</param>
    /// <param name="maxRecords">Máximo número de registros a procesar (0 = ilimitado)</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Lista de awards extraídos</returns>
    public async Task<List<Award>> DownloadAwardsAsync(
        int year,
        int maxRecords = 0,
        CancellationToken cancellationToken = default)
    {
        var downloadUrl = $"{BaseDownloadUrl}?name={year}.jsonl.gz";

        _logger.LogInformation("📥 Descargando datos OCDS de ChileCompra: {Year}", year);
        _logger.LogInformation("🔗 URL: {Url}", downloadUrl);

        try
        {
            // Descargar archivo comprimido
            using var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var contentLength = response.Content.Headers.ContentLength;
            _logger.LogInformation("📦 Tamaño del archivo: {Size} MB",
                contentLength.HasValue ? (contentLength.Value / 1024.0 / 1024.0).ToString("F2") : "desconocido");

            // Descomprimir y procesar stream
            await using var compressedStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
            using var reader = new StreamReader(gzipStream);

            var awards = new List<Award>();
            int lineNumber = 0;
            int processedRecords = 0;

            _logger.LogInformation("🔄 Procesando registros JSONL...");

            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                lineNumber++;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                try
                {
                    // Configurar opciones de deserialización (case-insensitive)
                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    // JSONL: Cada línea es un release directamente (no un ReleasePackage)
                    var release = JsonSerializer.Deserialize<OCDSRelease>(line, jsonOptions);

                    if (release?.Awards != null && release.Awards.Count > 0)
                    {
                        // Usar la fecha del release como publishedDate
                        var publishedDate = ParseDate(release.Date);
                        var extractedAwards = ExtractAwards(release, publishedDate);
                        awards.AddRange(extractedAwards);
                    }

                    processedRecords++;

                    // Log de progreso cada 1000 registros
                    if (processedRecords % 1000 == 0)
                    {
                        _logger.LogInformation("⏳ Procesados: {Count} registros, {Awards} awards extraídos",
                            processedRecords, awards.Count);
                    }

                    // Límite de registros alcanzado
                    if (maxRecords > 0 && processedRecords >= maxRecords)
                    {
                        _logger.LogInformation("🛑 Límite de {Max} registros alcanzado", maxRecords);
                        break;
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning("⚠️ Error parseando línea {Line}: {Error}", lineNumber, ex.Message);
                }
            }

            _logger.LogInformation("✅ Descarga completada: {Records} registros procesados, {Awards} awards extraídos",
                processedRecords, awards.Count);

            return awards;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "❌ Error HTTP descargando datos de ChileCompra");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error inesperado descargando datos");
            throw;
        }
    }

    /// <summary>
    /// Extrae awards de un release OCDS y los convierte a entidades del dominio.
    /// </summary>
    private List<Award> ExtractAwards(OCDSRelease release, DateTime publishedDate)
    {
        var awards = new List<Award>();

        if (release.Awards == null || !release.Awards.Any())
            return awards;

        foreach (var awardData in release.Awards)
        {
            try
            {
                var awardDate = ParseDate(awardData.Date);

                var award = new Award
                {
                    Id = Guid.NewGuid(),
                    Ocid = release.Ocid ?? string.Empty,
                    AwardId = awardData.Id ?? string.Empty,
                    Title = awardData.Title ?? string.Empty,
                    Status = awardData.Status ?? "unknown",
                    AwardDate = awardDate,
                    Amount = awardData.Value?.Amount ?? 0m,
                    Currency = awardData.Value?.Currency ?? "CLP",
                    PublishedDate = publishedDate,
                    Periodo = awardDate.ToString("yyyy-MM")
                };

                // Extraer información del supplier
                if (awardData.Suppliers != null && awardData.Suppliers.Any())
                {
                    var supplier = awardData.Suppliers.First();
                    award.SupplierName = supplier.Name ?? string.Empty;
                    award.SupplierRut = supplier.Identifier?.Id ?? string.Empty;
                }

                // Extraer nombre del comprador si está disponible en parties
                if (release.Parties != null)
                {
                    var buyer = release.Parties.FirstOrDefault(p => p.Roles?.Contains("buyer") == true);
                    if (buyer != null)
                    {
                        award.BuyerName = buyer.Name ?? string.Empty;
                    }
                }

                awards.Add(award);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("⚠️ Error extrayendo award {AwardId}: {Error}",
                    awardData.Id, ex.Message);
            }
        }

        return awards;
    }

    /// <summary>
    /// Parsea string de fecha en formato ISO 8601.
    /// </summary>
    private DateTime ParseDate(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
            return DateTime.MinValue;

        if (DateTime.TryParse(dateString, out var date))
            return date;

        return DateTime.MinValue;
    }

    #region Clases auxiliares para deserialización JSON (OCDS Schema)

    public class OCDSReleasePackage
    {
        public string? Uri { get; set; }
        public DateTime PublishedDate { get; set; }
        public List<OCDSRelease>? Releases { get; set; }
    }

    public class OCDSRelease
    {
        public string? Ocid { get; set; }
        public string? Id { get; set; }
        public string? Date { get; set; }
        public List<OCDSAward>? Awards { get; set; }
        public List<OCDSParty>? Parties { get; set; }
    }

    public class OCDSAward
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public string? Status { get; set; }
        public string? Date { get; set; }
        public OCDSValue? Value { get; set; }
        public List<OCDSSupplier>? Suppliers { get; set; }
    }

    public class OCDSValue
    {
        public decimal Amount { get; set; }
        public string? Currency { get; set; }
    }

    public class OCDSSupplier
    {
        public string? Name { get; set; }
        public OCDSIdentifier? Identifier { get; set; }
    }

    public class OCDSIdentifier
    {
        public string? Id { get; set; }
        public string? Scheme { get; set; }
    }

    public class OCDSParty
    {
        public string? Name { get; set; }
        public List<string>? Roles { get; set; }
    }

    #endregion
}
