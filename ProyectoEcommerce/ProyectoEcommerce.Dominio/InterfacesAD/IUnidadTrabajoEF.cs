using System.Data;
using ProyectoEcommerce.Dominio.Entidades;

namespace ProyectoEcommerce.Dominio.InterfacesAD
{
    /// <summary>
    /// Agrupa los repositorios y controla la transacción compartida del contexto de Entity Framework.
    /// </summary>
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
        /// <summary>Guarda los cambios pendientes sin cerrar la unidad de trabajo.</summary>
        int Completar();
        /// <summary>Guarda y confirma la transacción iniciada.</summary>
        void CompletarTran();
        /// <summary>Inicia una transacción con el nivel de aislamiento solicitado.</summary>
        void EmpezarTransaccion(IsolationLevel nivelAislamiento = IsolationLevel.ReadCommitted);
        /// <summary>Revierte la transacción activa.</summary>
        void Rollback();
        /// <summary>Cierra la conexión de base de datos asociada al contexto.</summary>
        void CerrarConexion();
    }
}
