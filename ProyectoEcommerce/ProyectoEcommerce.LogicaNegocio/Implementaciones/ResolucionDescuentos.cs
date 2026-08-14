using ProyectoEcommerce.Dominio.EntidadesTipadas;

namespace ProyectoEcommerce.LogicaNegocio.Implementaciones;

// recibe todos los descuentos que podria tener un producto
// escoge uno solo de forma estable y calcula el precio final
public static class ResolucionDescuentos
{
    // devuelve true si esta activo y la fecha actual cae entre inicio y fin incluyendo los extremos
    public static bool EstaVigente(bool activo, DateTime fechaInicio, DateTime fechaFin, DateTime ahora) =>
        activo && fechaInicio <= ahora && ahora <= fechaFin;

    // traduce las fechas y el estado a la etiqueta que se muestra en administracion
    public static string EstadoVigencia(bool activo, DateTime fechaInicio, DateTime fechaFin, DateTime ahora) =>
        !activo ? "Inactivo" : ahora < fechaInicio ? "Programado" : ahora > fechaFin ? "Finalizado" : "Vigente";

    // recibe precio y candidatos, calcula cuanto rebaja cada uno y devuelve el que deja menor precio
    public static TDescuentoAplicado Calcular(
        int productoId,
        decimal precioOriginal,
        IEnumerable<TDescuentoCandidato> candidatos)
    {
        if (precioOriginal < 0) throw new ArgumentOutOfRangeException(nameof(precioOriginal));

        // Where deja porcentajes validos
        // cada Select va agregando monto y precio final sin cambiar los candidatos originales
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
            // primero gana el menor precio, luego el tipo mas especifico y al final el ID menor
            .OrderBy(x => x.PrecioFinal)
            .ThenBy(x => Prioridad(x.Candidato.TipoDescuento))
            .ThenBy(x => x.Candidato.DescuentoId)
            // FirstOrDefault devuelve null cuando no quedo ningun descuento valido
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

    // si dos descuentos dejan el mismo precio este orden decide cual nombre se muestra
    private static int Prioridad(string tipoDescuento) => tipoDescuento switch
    {
        "PROMOCIONAL" => 1,
        "PRODUCTO" => 2,
        "CATEGORIA" => 3,
        "FAMILIA" => 4,
        _ => int.MaxValue
    };

    // redondea dinero a dos decimales y los medios centimos se alejan de cero
    private static decimal Redondear(decimal valor) =>
        Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}
