using Bogus;
using BlockSync.Domain.Entities;

namespace BlockSync.Infrastructure.Services;

/// <summary>
/// Generador de datos de prueba usando Bogus.
/// Usa un seed fijo (8675309) para garantizar datos reproducibles entre ejecuciones.
/// </summary>
public class DataGenerator
{
    private const int SEED = 8675309; // Seed fijo para reproducibilidad
    private const int TOTAL_RECORDS = 1000000; // Total de ventas a generar

    /// <summary>
    /// Genera 5,000 ventas de prueba con datos aleatorios pero reproducibles.
    /// Las fechas van desde 2022-01-01 hasta la fecha actual.
    /// </summary>
    public static List<Venta> GenerateVentas()
    {
        // Configurar generador con seed fijo
        Randomizer.Seed = new Random(SEED);

        var faker = new Faker<Venta>("es")
            .RuleFor(v => v.Id, f => Guid.NewGuid())
            .RuleFor(v => v.FechaVenta, f => f.Date.Between(new DateTime(2022, 1, 1), DateTime.Now))
            .RuleFor(v => v.Cliente, f => f.Company.CompanyName())
            .RuleFor(v => v.Producto, f => f.Commerce.ProductName())
            .RuleFor(v => v.Monto, f => f.Finance.Amount(10, 5000, 2));

        var ventas = faker.Generate(TOTAL_RECORDS);

        // Calcular y asignar el periodo para cada venta
        foreach (var venta in ventas)
        {
            venta.Periodo = $"{venta.FechaVenta.Year:D4}-{venta.FechaVenta.Month:D2}";
        }

        return ventas;
    }

    /// <summary>
    /// Genera ventas agrupadas por periodo para facilitar el procesamiento por bloques
    /// </summary>
    public static Dictionary<string, List<Venta>> GenerateVentasGroupedByPeriod()
    {
        var ventas = GenerateVentas();
        return ventas.GroupBy(v => v.Periodo)
                     .ToDictionary(g => g.Key, g => g.ToList());
    }
}
