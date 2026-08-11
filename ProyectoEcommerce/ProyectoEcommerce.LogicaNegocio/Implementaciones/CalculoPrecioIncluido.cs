namespace ProyectoEcommerce.LogicaNegocio.Implementaciones;

/// <summary>
/// Separa base imponible e impuesto a partir de un precio final que ya incluye el impuesto.
/// </summary>
public static class CalculoPrecioIncluido
{
    /// <summary>Calcula el desglose fiscal sin volver a sumar el impuesto al total del Cliente.</summary>
    public static DesglosePrecioIncluido Calcular(
        decimal precioFinalUnitario,
        int cantidad,
        decimal porcentajeImpuesto,
        decimal porcentajeDescuento = 0m)
    {
        if (precioFinalUnitario < 0) throw new ArgumentOutOfRangeException(nameof(precioFinalUnitario));
        if (cantidad <= 0) throw new ArgumentOutOfRangeException(nameof(cantidad));
        if (porcentajeImpuesto is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(porcentajeImpuesto));
        if (porcentajeDescuento is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(porcentajeDescuento));

        // PrecioVenta representa el precio final. La división posterior solo obtiene
        // la base imponible necesaria para órdenes y facturas.
        var importeFinal = Redondear(precioFinalUnitario * cantidad);
        var descuento = Redondear(importeFinal * porcentajeDescuento / 100m);
        var total = Redondear(importeFinal - descuento);
        var subtotal = porcentajeImpuesto == 0m
            ? total
            : Redondear(total / (1m + porcentajeImpuesto / 100m));
        var impuestos = total - subtotal;

        return new DesglosePrecioIncluido(subtotal, impuestos, descuento, total);
    }

    private static decimal Redondear(decimal valor) =>
        Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}

public readonly record struct DesglosePrecioIncluido(
    decimal Subtotal,
    decimal Impuestos,
    decimal Descuento,
    decimal Total);
