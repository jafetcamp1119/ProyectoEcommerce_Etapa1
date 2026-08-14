using System.Data;
using ProyectoEcommerce.Dominio.Entidades;

namespace ProyectoEcommerce.Dominio.InterfacesAD
{
    // este contrato junta todos los repositorios que usan el mismo contexto
    // tambien deja empezar, confirmar o devolver una transaccion completa
    public interface IUnidadTrabajoEF : IDisposable
    {
        IRepositorioAD<FamiliaProducto> TFamiliaProducto { get; }
        IRepositorioAD<Categoria> TCategoria { get; }
        IRepositorioAD<Impuesto> TImpuesto { get; }
        IRepositorioAD<Producto> TProducto { get; }
        IRepositorioAD<Usuario> TUsuario { get; }
        IRepositorioAD<Rol> TRol { get; }
        IRepositorioAD<HistorialAcceso> THistorialAcceso { get; }
        IRepositorioAD<ProductoImagen> TProductoImagen { get; }
        IRepositorioAD<MenuOpcion> TMenuOpcion { get; }
        IRepositorioAD<RolMenuOpcion> TRolMenuOpcion { get; }
        IRepositorioAD<BitacoraSistema> TBitacoraSistema { get; }
        IRepositorioAD<Orden> TOrden { get; }
        IRepositorioAD<OrdenDetalle> TOrdenDetalle { get; }
        IRepositorioAD<Carrito> TCarrito { get; }
        IRepositorioAD<CarritoDetalle> TCarritoDetalle { get; }
        IRepositorioAD<Descuento> TDescuento { get; }
        IRepositorioAD<Proveedor> TProveedor { get; }
        IRepositorioAD<ProveedorCategoria> TProveedorCategoria { get; }
        IRepositorioAD<ProductoProveedorCatalogo> TProductoProveedorCatalogo { get; }
        IRepositorioAD<ProductoProveedor> TProductoProveedor { get; }
        // manda a la BD los cambios que quedaron pendientes
        int Completar();
        // guarda y hace Commit de la transaccion que se inicio antes
        void CompletarTran();
        // abre una transaccion para que varios cambios se guarden juntos o ninguno
        void EmpezarTransaccion(IsolationLevel nivelAislamiento = IsolationLevel.ReadCommitted);
        // hace Rollback y devuelve todo lo que todavia no se habia confirmado
        void Rollback();
        // cierra la conexion que usa Entity Framework
        void CerrarConexion();
    }
}
