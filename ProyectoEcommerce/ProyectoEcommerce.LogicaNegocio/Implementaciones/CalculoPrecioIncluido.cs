namespace ProyectoEcommerce.LogicaNegocio.Implementaciones;

// separa subtotal e impuesto partiendo del precio final que ya paga el Cliente
// tambien descuenta el porcentaje sin volver a sumar el impuesto al total
public static class CalculoPrecioIncluido
{
    // recibe precio unitario final, cantidad, impuesto y descuento
    // devuelve subtotal, impuesto, descuento y total ya redondeados
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

        // PrecioVenta ya es el precio final, la division solo saca la base para ordenes y facturas
        var importeFinal = Redondear(precioFinalUnitario * cantidad);
        var descuento = Redondear(importeFinal * porcentajeDescuento / 100m);
        var total = Redondear(importeFinal - descuento);
        // cuando hay impuesto divide entre 1 + porcentaje para sacar la base sin impuesto
        var subtotal = porcentajeImpuesto == 0m
            ? total
            : Redondear(total / (1m + porcentajeImpuesto / 100m));
        var impuestos = total - subtotal;

        return new DesglosePrecioIncluido(subtotal, impuestos, descuento, total);
    }

    // usa la misma regla de redondeo para que carrito, orden y factura coincidan
    private static decimal Redondear(decimal valor) =>
        Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}

public readonly record struct DesglosePrecioIncluido(
    decimal Subtotal,
    decimal Impuestos,
    decimal Descuento,
    decimal Total);
