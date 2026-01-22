namespace BlockSync.Application.DTOs;

/// <summary>
/// Respuesta del endpoint /hashes que muestra comparación detallada de hashes
/// </summary>
public class HashComparisonResponse
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int TotalPeriodos { get; set; }
    public int Sincronizados { get; set; }
    public int FaltanEnDestino { get; set; }
    public int Corruptos { get; set; }
    public List<BlockComparison> Bloques { get; set; } = new();
}

/// <summary>
/// Comparación de un bloque específico entre origen y destino
/// </summary>
public class BlockComparison
{
    public string Periodo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty; // SINCRONIZADO, FALTA_EN_DESTINO, CORRUPTO
    public BlockInfo? Origen { get; set; }
    public BlockInfo? Destino { get; set; }
}

/// <summary>
/// Información de un bloque (origen o destino)
/// </summary>
public class BlockInfo
{
    public string Hash { get; set; } = string.Empty;
    public int Registros { get; set; }
    public decimal Monto { get; set; }
}
