using System.Globalization;
using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Dominio.InterfazLN;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ProyectoEcommerce.LogicaNegocio.Implementaciones;

// arma con QuestPDF el PDF que se guarda y se manda despues de una compra
public class FacturaLN : IFacturaLN
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-CR");

    public FacturaLN()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    // recibe los datos historicos de la orden y devuelve el PDF como arreglo de bytes
    public byte[] Generar(TFacturaDatos factura)
    {
        ArgumentNullException.ThrowIfNull(factura);

        // Document.Create empieza a describir el tamaño y los bloques de cada pagina
        return Document.Create(documento =>
        {
            documento.Page(pagina =>
            {
                pagina.Size(PageSizes.A4);
                pagina.Margin(32);
                pagina.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Darken3));

                // el encabezado coloca LessPrice a la izquierda y los datos de factura a la derecha
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

                // el contenido junta cliente, tabla de productos, totales y metodo de pago
                pagina.Content().PaddingVertical(16).Column(columna =>
                {
                    columna.Spacing(14);
                    columna.Item().Element(contenedor => DatosCliente(contenedor, factura));
                    columna.Item().Element(contenedor => TablaProductos(contenedor, factura.Items));
                    // base e impuesto incluido se muestran separados pero no se vuelven a sumar al total
                    columna.Item().AlignRight().Width(280).Element(contenedor => Totales(contenedor, factura));
                    columna.Item().Background(Colors.Grey.Lighten4).Padding(10).Column(nota =>
                    {
                        nota.Item().Text($"Método de pago: {EtiquetaMetodo(factura.MetodoPago)}").SemiBold();
                    });
                });

                // CurrentPageNumber y TotalPages ponen la numeracion automaticamente
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

    // dibuja la caja con nombre, correo y direccion de entrega
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

    // define las columnas y recorre los productos historicos de la orden
    private static void TablaProductos(IContainer contenedor, IReadOnlyCollection<TFacturaItem> items)
    {
        contenedor.Table(tabla =>
        {
            // RelativeColumn usa el espacio sobrante y ConstantColumn reserva un ancho fijo
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

            // agrega una fila visual por cada producto comprado
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

    // muestra el desglose y resalta el total final que ya incluye el impuesto
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
                fila.RelativeItem().PaddingTop(7).AlignRight().Text(
                Moneda(factura.Total)).FontSize(11).SemiBold().FontColor(Colors.Blue.Darken2);
            });
        });
    }

    // dibuja una fila comun de etiqueta y monto alineado a la derecha
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

    // N2 usa separadores de Costa Rica y siempre muestra dos decimales
    private static string Moneda(decimal valor) => $"CRC {valor.ToString("N2", Cultura)}";

    // calcula el precio unitario despues del descuento para mostrarlo en la tabla
    private static decimal PrecioAplicado(TFacturaItem item) =>
        Math.Round(item.PrecioUnitario * (1m - item.PorcentajeDescuento / 100m), 2, MidpointRounding.AwayFromZero);
    // convierte el codigo guardado a un texto mas agradable para la factura
    private static string EtiquetaMetodo(string metodo) => metodo.Equals("TARJETA", StringComparison.OrdinalIgnoreCase) ? "Tarjeta" : "Efectivo";
}
