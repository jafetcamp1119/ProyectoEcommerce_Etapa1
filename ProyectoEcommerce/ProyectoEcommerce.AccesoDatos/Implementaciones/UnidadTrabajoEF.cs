using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using Microsoft.Extensions.Configuration;
using ProyectoEcommerce.AccesoDatos.Contexto;
using ProyectoEcommerce.Dominio.Entidades;
using ProyectoEcommerce.Dominio.InterfacesAD;

namespace ProyectoEcommerce.AccesoDatos.Implementaciones
{
    /// <summary>
    /// Implementa la unidad de trabajo que comparte un DbContext entre repositorios
    /// y permite confirmar o revertir operaciones que deben ser atómicas.
    /// </summary>
    public class UnidadTrabajoEF : IUnidadTrabajoEF
    {
        #region "Atributos y Variables"

        private ProyectoEcommerceContext _Contexto { get; set; }
        private IConfiguration _configuration { get; set; }
        private IDbContextTransaction? _transaction = null;

        private RepositorioAD<FamiliaProducto>? _TFamiliaProducto;
        private RepositorioAD<Categoria>? _TCategoria;
        private RepositorioAD<Impuesto>? _TImpuesto;
        private RepositorioAD<Producto>? _TProducto;
        private RepositorioAD<Usuario>? _TUsuario;
        private RepositorioAD<Rol>? _TRol;
        private RepositorioAD<HistorialAcceso>? _THistorialAcceso;
        private RepositorioAD<ProductoImagen>? _TProductoImagen;
        private RepositorioAD<MenuOpcion>? _TMenuOpcion;
        private RepositorioAD<RolMenuOpcion>? _TRolMenuOpcion;
        private RepositorioAD<BitacoraSistema>? _TBitacoraSistema;
        private RepositorioAD<Orden>? _TOrden;
        private RepositorioAD<OrdenDetalle>? _TOrdenDetalle;
        private RepositorioAD<Carrito>? _TCarrito;
        private RepositorioAD<CarritoDetalle>? _TCarritoDetalle;
        private RepositorioAD<Descuento>? _TDescuento;

        #endregion

        #region "Constructores"

        public UnidadTrabajoEF(ProyectoEcommerceContext Contexto, IConfiguration configuration)
        {
            _Contexto = Contexto;
            _configuration = configuration;
        }

        // Cada repositorio se crea bajo demanda y reutiliza el mismo contexto de la solicitud.
        public IRepositorioAD<FamiliaProducto> TFamiliaProducto =>
            _TFamiliaProducto ??= new RepositorioAD<FamiliaProducto>(_Contexto);

        public IRepositorioAD<Categoria> TCategoria =>
            _TCategoria ??= new RepositorioAD<Categoria>(_Contexto);

        public IRepositorioAD<Impuesto> TImpuesto =>
            _TImpuesto ??= new RepositorioAD<Impuesto>(_Contexto);

        public IRepositorioAD<Producto> TProducto =>
            _TProducto ??= new RepositorioAD<Producto>(_Contexto);

        public IRepositorioAD<Usuario> TUsuario =>
            _TUsuario ??= new RepositorioAD<Usuario>(_Contexto);

        public IRepositorioAD<Rol> TRol =>
            _TRol ??= new RepositorioAD<Rol>(_Contexto);

        public IRepositorioAD<HistorialAcceso> THistorialAcceso =>
            _THistorialAcceso ??= new RepositorioAD<HistorialAcceso>(_Contexto);

        public IRepositorioAD<ProductoImagen> TProductoImagen =>
            _TProductoImagen ??= new RepositorioAD<ProductoImagen>(_Contexto);

        public IRepositorioAD<MenuOpcion> TMenuOpcion =>
            _TMenuOpcion ??= new RepositorioAD<MenuOpcion>(_Contexto);

        public IRepositorioAD<RolMenuOpcion> TRolMenuOpcion =>
            _TRolMenuOpcion ??= new RepositorioAD<RolMenuOpcion>(_Contexto);

        public IRepositorioAD<BitacoraSistema> TBitacoraSistema =>
            _TBitacoraSistema ??= new RepositorioAD<BitacoraSistema>(_Contexto);

        public IRepositorioAD<Orden> TOrden =>
            _TOrden ??= new RepositorioAD<Orden>(_Contexto);

        public IRepositorioAD<OrdenDetalle> TOrdenDetalle =>
            _TOrdenDetalle ??= new RepositorioAD<OrdenDetalle>(_Contexto);

        public IRepositorioAD<Carrito> TCarrito =>
            _TCarrito ??= new RepositorioAD<Carrito>(_Contexto);

        public IRepositorioAD<CarritoDetalle> TCarritoDetalle =>
            _TCarritoDetalle ??= new RepositorioAD<CarritoDetalle>(_Contexto);

        public IRepositorioAD<Descuento> TDescuento =>
            _TDescuento ??= new RepositorioAD<Descuento>(_Contexto);

        public int Completar()
        {
            try
            {
                return _Contexto.SaveChanges();
            }
            catch
            {
                throw;
            }
        }

        /// <summary>Guarda cambios y confirma la transacción; ante un error realiza rollback.</summary>
        public void CompletarTran()
        {
            try
            {
                _Contexto.SaveChanges();
                _transaction!.Commit();
            }
            catch
            {
                _transaction?.Rollback();
                throw;
            }
        }

        public void EmpezarTransaccion(IsolationLevel nivelAislamiento = IsolationLevel.ReadCommitted)
        {
            _transaction = _Contexto.Database.BeginTransaction(nivelAislamiento);
        }

        public void Rollback()
        {
            _transaction?.Rollback();
        }

        public void CerrarConexion()
        {
            _Contexto.Database.CloseConnection();
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _Contexto.Dispose();
        }

        #endregion
    }
}
