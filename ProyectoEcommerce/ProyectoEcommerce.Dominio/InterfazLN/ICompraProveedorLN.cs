using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Utilidades;

namespace ProyectoEcommerce.Dominio.InterfazLN;

public interface ICompraProveedorLN
{
    Task<Respuesta<TDocumentoCompraProveedor>> PrepararProformaAsync(TSolicitudCompraProveedor datos);
    Task<Respuesta<bool>> EnviarProformaAsync(TSolicitudCompraProveedor datos, CancellationToken cancellationToken = default);
    Task<Respuesta<TCompraProveedorConfirmada>> ConfirmarAsync(
        TSolicitudCompraProveedor datos,
        int administradorId,
        CancellationToken cancellationToken = default);
    Task<Respuesta<TPagina<TCompraProveedorResumen>>> ListarAsync(TFiltroComprasProveedor filtro);
    Task<Respuesta<TCompraProveedorDetalleConsulta>> ObtenerDetalleAsync(int compraProveedorId);
    Task<Respuesta<TArchivoCompraProveedor>> ObtenerPdfAsync(int compraProveedorId);
}
