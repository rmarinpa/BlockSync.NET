namespace BlockSync.Application.DTOs;

/// <summary>
/// Respuesta del endpoint POST /reset que reinicia el sistema
/// </summary>
public class ResetResponse
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public List<string> AccionesRealizadas { get; set; } = new();
    public string SiguientePaso { get; set; } = "Ejecuta POST /sync para sincronizar desde cero";
}
