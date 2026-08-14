using ProyectoEcommerce.Dominio.EntidadesTipadas;

namespace ProyectoEcommerce.Dominio.InterfazLN;

// recibe los datos historicos de una compra y devuelve el PDF en memoria
public interface IFacturaLN
{
    // el arreglo de bytes despues se puede guardar en disco o mandar en la respuesta HTTP
    byte[] Generar(TFacturaDatos factura);
}
