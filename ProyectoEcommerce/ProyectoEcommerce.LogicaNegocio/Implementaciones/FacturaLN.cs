using System.Globalization;
using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Dominio.InterfazLN;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ProyectoEcommerce.LogicaNegocio.Implementaciones;

/// <summary>
/// Construye con QuestPDF la representación imprimible de una factura de LessPrice.
/// </summary>
public class FacturaLN : IFacturaLN
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-CR");

    public FacturaLN()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>Genera el PDF en memoria usando los datos históricos capturados en la orden.</summary>
    public byte[] Generar(TFacturaDatos factura)
    {
        ArgumentNullException.ThrowIfNull(factura);

        return Document.Create(documento =>
        {
            documento.Page(pagina =>
            {
                pagina.Size(PageSizes.A4);
                pagina.Margin(32);
                pagina.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Darken3));

                pagina.Header().Column(columna =>
                {
                    columna.Item().Row(fila =>
                    {
                        fila.RelativeItem().Column(empresa =>
                        {
                            empresa.Item().Text("LESSPRICE").FontSize(18).SemiBold().FontColor(Colors.Blue.Darken2);
                        });
                        fila.ConstantItem(190).AlignRight().Column(datos =>
                        {
                            datos.Item().Text($"FACTURA #{factura.NumeroFactura}").FontSize(13).SemiBold();
                            datos.Item().Text($"Orden #{factura.NumeroOrden}");
                            datos.Item().Text(factura.Fecha.ToString("dd/MM/yyyy HH:mm", Cultura));
                        });
                    });
                    columna.Item().PaddingTop(12).LineHorizontal(1).LineColor(Colors.Blue.Lighten1);
                });

                pagina.Content().PaddingVertical(16).Column(columna =>
                {
                    columna.Spacing(14);
                    columna.Item().Element(contenedor => DatosCliente(contenedor, factura));
                    columna.Item().Element(contenedor => TablaProductos(contenedor, factura.Items));
                    // La base y el impuesto incluido se muestran separados, pero no se vuelven a sumar al total final.
                    columna.Item().AlignRight().Width(280).Element(contenedor => Totales(contenedor, factura));
                    columna.Item().Background(Colors.Grey.Lighten4).Padding(10).Column(nota =>
                    {
                        nota.Item().Text($"Método de pago: {EtiquetaMetodo(factura.MetodoPago)}").SemiBold();
                    });
                });

                pagina.Footer().AlignCenter().Text(texto =>
                {
                    texto.Span("Página ");
                    texto.CurrentPageNumber();
                    texto.Span(" de ");
                    texto.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    private static void DatosCliente(IContainer contenedor, TFacturaDatos factura)
    {
        contenedor.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(columna =>
        {
            columna.Spacing(4);
            columna.Item().Text("DATOS DEL CLIENTE").SemiBold().FontColor(Colors.Blue.Darken2);
            columna.Item().Text(texto => { texto.Span("Cliente: ").SemiBold(); texto.Span(factura.Cliente); });
            columna.Item().Text(texto => { texto.Span("Correo: ").SemiBold(); texto.Span(factura.Correo); });
            columna.Item().Text(texto => { texto.Span("Dirección de envío: ").SemiBold(); texto.Span(factura.DireccionEnvio); });
        });
    }

    private static void TablaProductos(IContainer contenedor, IReadOnlyCollection<TFacturaItem> items)
    {
        contenedor.Table(tabla =>
        {
            tabla.ColumnsDefinition(columnas =>
            {
                columnas.RelativeColumn(2.3f);
                columnas.ConstantColumn(34);
                columnas.ConstantColumn(70);
                columnas.ConstantColumn(48);
                columnas.ConstantColumn(70);
                columnas.ConstantColumn(48);
                columnas.ConstantColumn(75);
            });

            tabla.Header(encabezado =>
            {
                Encabezado(encabezado.Cell(), "Producto");
                Encabezado(encabezado.Cell().AlignCenter(), "Cant.");
                Encabezado(encabezado.Cell().AlignRight(), "Precio original");
                Encabezado(encabezado.Cell().AlignRight(), "Desc.");
                Encabezado(encabezado.Cell().AlignRight(), "Precio aplicado");
                Encabezado(encabezado.Cell().AlignRight(), "Imp.");
                Encabezado(encabezado.Cell().AlignRight(), "Total");
            });

            foreach (var item in items)
            {
                Celda(tabla.Cell(), item.Nombre);
                Celda(tabla.Cell().AlignCenter(), item.Cantidad.ToString(Cultura));
                Celda(tabla.Cell().AlignRight(), Moneda(item.PrecioUnitario));
                Celda(tabla.Cell().AlignRight(), $"{item.PorcentajeDescuento:N2}%");
                Celda(tabla.Cell().AlignRight(), Moneda(PrecioAplicado(item)));
                Celda(tabla.Cell().AlignRight(), $"{item.PorcentajeImpuesto:N2}%");
                Celda(tabla.Cell().AlignRight(), Moneda(item.TotalLinea));
            }
        });
    }

    private static void Totales(IContainer contenedor, TFacturaDatos factura)
    {
        contenedor.Column(columna =>
        {
            FilaTotal(columna, "Subtotal antes de impuestos", factura.Subtotal);
            FilaTotal(columna, "Impuesto incluido", factura.Impuestos);
            FilaTotal(columna, "Descuentos", factura.Descuentos);
            columna.Item().PaddingTop(5).BorderTop(1).BorderColor(Colors.Grey.Lighten1).Row(fila =>
            {
                fila.RelativeItem().PaddingTop(7).Text("TOTAL").FontSize(11).SemiBold();
                fila.RelativeItem().PaddingTop(7).AlignRight().Text(Moneda(factura.Total)).FontSize(11).SemiBold().FontColor(Colors.Blue.Darken2);
            });
        });
    }

    private static void FilaTotal(ColumnDescriptor columna, string etiqueta, decimal valor)
    {
        columna.Item().PaddingVertical(3).Row(fila =>
        {
            fila.RelativeItem().Text(etiqueta);
            fila.RelativeItem().AlignRight().Text(Moneda(valor));
        });
    }

    private static void Encabezado(IContainer celda, string texto) =>
        celda.Background(Colors.Blue.Darken2).PaddingVertical(7).PaddingHorizontal(5).Text(texto).FontColor(Colors.White).SemiBold();

    private static void Celda(IContainer celda, string texto) =>
        celda.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(7).PaddingHorizontal(5).Text(texto);

    private static string Moneda(decimal valor) => $"CRC {valor.ToString("N2", Cultura)}";
    private static decimal PrecioAplicado(TFacturaItem item) =>
        Math.Round(item.PrecioUnitario * (1m - item.PorcentajeDescuento / 100m), 2, MidpointRounding.AwayFromZero);
    private static string EtiquetaMetodo(string metodo) => metodo.Equals("TARJETA", StringComparison.OrdinalIgnoreCase) ? "Tarjeta" : "Efectivo";
}
