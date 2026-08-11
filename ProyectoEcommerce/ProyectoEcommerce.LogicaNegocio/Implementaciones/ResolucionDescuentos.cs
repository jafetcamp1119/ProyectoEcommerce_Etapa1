using ProyectoEcommerce.Dominio.EntidadesTipadas;

namespace ProyectoEcommerce.LogicaNegocio.Implementaciones;

/// <summary>Selecciona un único descuento y calcula el precio final de forma determinista.</summary>
public static class ResolucionDescuentos
{
    /// <summary>Evalúa la ventana inclusiva requerida por todos los consumidores.</summary>
    public static bool EstaVigente(bool activo, DateTime fechaInicio, DateTime fechaFin, DateTime ahora) =>
        activo && fechaInicio <= ahora && ahora <= fechaFin;

    public static string EstadoVigencia(bool activo, DateTime fechaInicio, DateTime fechaFin, DateTime ahora) =>
        !activo ? "Inactivo" : ahora < fechaInicio ? "Programado" : ahora > fechaFin ? "Finalizado" : "Vigente";

    public static TDescuentoAplicado Calcular(
        int productoId,
        decimal precioOriginal,
        IEnumerable<TDescuentoCandidato> candidatos)
    {
        if (precioOriginal < 0) throw new ArgumentOutOfRangeException(nameof(precioOriginal));

        var evaluados = candidatos
            .Where(x => x.Porcentaje is > 0m and <= 100m)
            .Select(x => new
            {
                Candidato = x,
                Monto = Redondear(precioOriginal * x.Porcentaje / 100m)
            })
            .Select(x => new
            {
                x.Candidato,
                x.Monto,
                PrecioFinal = Redondear(precioOriginal - x.Monto)
            })
            .OrderBy(x => x.PrecioFinal)
            .ThenBy(x => Prioridad(x.Candidato.TipoDescuento))
            .ThenBy(x => x.Candidato.DescuentoId)
            .FirstOrDefault();

        if (evaluados == null)
        {
            return new TDescuentoAplicado
            {
                ProductoId = productoId,
                PrecioOriginal = precioOriginal,
                PrecioFinal = precioOriginal
            };
        }

        return new TDescuentoAplicado
        {
            ProductoId = productoId,
            TieneDescuento = true,
            DescuentoId = evaluados.Candidato.DescuentoId,
            TipoDescuento = evaluados.Candidato.TipoDescuento,
            Nombre = evaluados.Candidato.Nombre,
            Porcentaje = evaluados.Candidato.Porcentaje,
            PrecioOriginal = precioOriginal,
            MontoDescuento = evaluados.Monto,
            PrecioFinal = evaluados.PrecioFinal
        };
    }

    private static int Prioridad(string tipoDescuento) => tipoDescuento switch
    {
        "PROMOCIONAL" => 1,
        "PRODUCTO" => 2,
        "CATEGORIA" => 3,
        "FAMILIA" => 4,
        _ => int.MaxValue
    };

    private static decimal Redondear(decimal valor) =>
        Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}
