namespace BlockSync.Application.DTOs;

/// <summary>
/// Respuesta del endpoint POST /sync que ejecuta la sincronización
/// </summary>
public class SyncResponse
{
    public bool Exitoso { get; set; }
    public SyncSummary Resumen { get; set; } = new();
    public SyncReport? Reporte { get; set; }
    public string ResumenTexto { get; set; } = string.Empty;
}

/// <summary>
/// Resumen simplificado de la sincronización
/// </summary>
public class SyncSummary
{
    public long DuracionMs { get; set; }
    public int TotalBloques { get; set; }
    public int BloquesOmitidos { get; set; }
    public int BloquesInsertados { get; set; }
    public int BloquesReparados { get; set; }
    public int TotalRegistrosProcesados { get; set; }
    public int TotalRegistrosInsertados { get; set; }
}
