using ProyectoEcommerce.Dominio.EntidadesTipadas;

namespace ProyectoEcommerce.Dominio.InterfazLN;

/// <summary>Define la generación en memoria del documento PDF de una factura.</summary>
public interface IFacturaLN
{
    /// <summary>Genera los bytes del PDF a partir de los datos históricos de la orden.</summary>
    byte[] Generar(TFacturaDatos factura);
}
