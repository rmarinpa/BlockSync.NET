namespace BlockSync.Application.DTOs;

/// <summary>
/// Respuesta del endpoint POST /hack/{anio}/{mes} que corrompe datos para testing
/// </summary>
public class HackResponse
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public string PeriodoAfectado { get; set; } = string.Empty;
    public string Instrucciones { get; set; } = "Ejecuta POST /sync para detectar y reparar la corrupción";
}
