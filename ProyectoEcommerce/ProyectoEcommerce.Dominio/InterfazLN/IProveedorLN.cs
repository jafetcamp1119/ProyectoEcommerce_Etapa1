using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Utilidades;

namespace ProyectoEcommerce.Dominio.InterfazLN;

public interface IProveedorLN
{
    Task<Respuesta<IEnumerable<TProveedor>>> ListarAsync(bool soloActivos = false);
    Task<Respuesta<TProveedor>> ObtenerAsync(int proveedorId);
    Task<Respuesta<TProveedor>> InsertarAsync(TProveedor datos, int administradorId);
    Task<Respuesta<TProveedor>> ModificarAsync(TProveedor datos, int administradorId);
    Task<Respuesta<TProveedor>> CambiarEstadoAsync(int proveedorId, bool activo, int administradorId);
    Task<Respuesta<IEnumerable<TFamiliaOfertaProveedor>>> ListarFamiliasAsync(int proveedorId, bool soloDisponibles = false);
    Task<Respuesta<IEnumerable<TCategoriaOfertaProveedor>>> ListarCategoriasAsync(int proveedorId, int? familiaId, bool soloDisponibles = false);
    Task<Respuesta<IEnumerable<TProductoOfertaProveedor>>> ListarProductosAsync(int proveedorId, int? categoriaId, bool soloDisponibles = false);
    Task<Respuesta<IEnumerable<TProductoExistenteProveedor>>> ListarProductosExistentesAsync(
        int proveedorId,
        int categoriaId);
    Task<Respuesta<bool>> IncorporarFamiliaAsync(int proveedorId, int familiaId, int administradorId);
    Task<Respuesta<bool>> IncorporarCategoriaAsync(int proveedorId, int categoriaId, int administradorId);
    Task<Respuesta<TCategoriaProveedorResultado>> AsociarCategoriaExistenteAsync(
        int proveedorId,
        int categoriaId,
        int administradorId);
    Task<Respuesta<TCategoriaProveedorResultado>> CrearCategoriaAsync(
        int proveedorId,
        TCategoriaNuevaProveedor datos,
        int administradorId);
    Task<Respuesta<TProductoOfertaProveedor>> AgregarProductoExistenteAsync(
        int proveedorId,
        int categoriaId,
        TAgregarProductoExistenteProveedor datos,
        int administradorId);
    Task<Respuesta<TProductoOfertaProveedor>> CrearProductoAsync(
        int proveedorId,
        int categoriaId,
        TCrearProductoProveedor datos,
        int administradorId);
    Task<Respuesta<TProductoOfertaProveedor>> ModificarProductoAsync(
        int ofertaId,
        TModificarProductoProveedor datos,
        int administradorId);
    Task<Respuesta<TProductoIncorporadoProveedor>> IncorporarProductoAsync(
        int ofertaId,
        TIncorporarProductoProveedor datos,
        int administradorId);
}
