namespace BlockSync.Application.DTOs;

/// <summary>
/// Respuesta del endpoint /status que muestra el estado de origen y destino
/// </summary>
public class SystemStatusResponse
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Sistema { get; set; } = "BlockSync.NET";
    public string Version { get; set; } = "1.0.0";
    public SystemInfo Origen { get; set; } = new();
    public SystemInfo Destino { get; set; } = new();
    public string EstadoSincronizacion { get; set; } = "DESCONOCIDO";
}

/// <summary>
/// Información de un sistema (origen o destino)
/// </summary>
public class SystemInfo
{
    public int Registros { get; set; }
    public int Bloques { get; set; }
    public List<string> Periodos { get; set; } = new();
}
