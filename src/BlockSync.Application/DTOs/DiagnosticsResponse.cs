namespace BlockSync.Application.DTOs;

/// <summary>
/// Respuesta del endpoint /diagnostics que demuestra que los datos son reales
/// </summary>
public class DiagnosticsResponse
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Sistema { get; set; } = "BlockSync.NET";
    public DataStatistics Origen { get; set; } = new();
    public DataStatistics Destino { get; set; } = new();
    public List<SampleRecord> MuestraAleatoria { get; set; } = new();
    public MemoryInfo Memoria { get; set; } = new();
    public string Resumen { get; set; } = string.Empty;
}

/// <summary>
/// Estadísticas detalladas de un repositorio
/// </summary>
public class DataStatistics
{
    public int TotalRegistros { get; set; }
    public int TotalBloques { get; set; }
    public decimal MontoTotal { get; set; }
    public decimal MontoPromedio { get; set; }
    public decimal MontoMinimo { get; set; }
    public decimal MontoMaximo { get; set; }
    public string PrimerRegistroId { get; set; } = string.Empty;
    public string UltimoRegistroId { get; set; } = string.Empty;
    public DateTime FechaMinima { get; set; }
    public DateTime FechaMaxima { get; set; }
    public int ClientesUnicos { get; set; }
    public int ProductosUnicos { get; set; }
    public List<PeriodInfo> Top10PeriodosPorRegistros { get; set; } = new();
}

/// <summary>
/// Información de un periodo específico
/// </summary>
public class PeriodInfo
{
    public string Periodo { get; set; } = string.Empty;
    public int Registros { get; set; }
    public decimal MontoTotal { get; set; }
    public string Hash { get; set; } = string.Empty;
}

/// <summary>
/// Muestra aleatoria de un registro para demostrar que no son hardcodeados
/// </summary>
public class SampleRecord
{
    public string Id { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public string Producto { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public string Periodo { get; set; } = string.Empty;
}

/// <summary>
/// Información de memoria utilizada
/// </summary>
public class MemoryInfo
{
    public long MemoriaUsadaMB { get; set; }
    public long MemoriaTotalMB { get; set; }
    public int CollectionsGC0 { get; set; }
    public int CollectionsGC1 { get; set; }
    public int CollectionsGC2 { get; set; }
}
