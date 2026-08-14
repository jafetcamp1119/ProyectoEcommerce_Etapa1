using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using Microsoft.Extensions.Configuration;
using ProyectoEcommerce.AccesoDatos.Contexto;
using ProyectoEcommerce.Dominio.Entidades;
using ProyectoEcommerce.Dominio.InterfacesAD;

namespace ProyectoEcommerce.AccesoDatos.Implementaciones
{
    // esta clase guarda un solo contexto para todos los repositorios de la misma solicitud
    // tambien sirve para confirmar o deshacer procesos que usan una transaccion
    public class UnidadTrabajoEF : IUnidadTrabajoEF
    {
        #region "Atributos y Variables"

        private ProyectoEcommerceContext _Contexto { get; set; }
        private IConfiguration _configuration { get; set; }
        // aqui se guarda la transaccion activa para poder hacer Commit o Rollback despues
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
        private RepositorioAD<Proveedor>? _TProveedor;
        private RepositorioAD<ProveedorCategoria>? _TProveedorCategoria;
        private RepositorioAD<ProductoProveedorCatalogo>? _TProductoProveedorCatalogo;
        private RepositorioAD<ProductoProveedor>? _TProductoProveedor;

        #endregion

        #region "Constructores"

        public UnidadTrabajoEF(ProyectoEcommerceContext Contexto, IConfiguration configuration)
        {
            // el contexto llega configurado desde Program.cs y se reutiliza en toda esta unidad
            _Contexto = Contexto;
            _configuration = configuration;
        }

        // ??= crea cada repositorio solamente la primera vez que se pide
        // todos reciben el mismo contexto para que los cambios pertenezcan a la misma operacion
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

        public IRepositorioAD<Proveedor> TProveedor =>
            _TProveedor ??= new RepositorioAD<Proveedor>(_Contexto);

        public IRepositorioAD<ProveedorCategoria> TProveedorCategoria =>
            _TProveedorCategoria ??= new RepositorioAD<ProveedorCategoria>(_Contexto);

        public IRepositorioAD<ProductoProveedorCatalogo> TProductoProveedorCatalogo =>
            _TProductoProveedorCatalogo ??= new RepositorioAD<ProductoProveedorCatalogo>(_Contexto);

        public IRepositorioAD<ProductoProveedor> TProductoProveedor =>
            _TProductoProveedor ??= new RepositorioAD<ProductoProveedor>(_Contexto);

        public int Completar()
        {
            try
            {
                // SaveChanges manda a SQL los cambios que Entity Framework tiene pendientes
                return _Contexto.SaveChanges();
            }
            catch
            {
                throw;
            }
        }

        // guarda lo pendiente y hace Commit para dejar fija la transaccion
        // si algo falla hace Rollback para no guardar el proceso a medias
        public void CompletarTran()
        {
            try
            {
                _Contexto.SaveChanges();
                // Commit confirma de forma definitiva todos los cambios de la transaccion
                _transaction!.Commit();
            }
            catch
            {
                // Rollback regresa la BD al estado que tenia antes de empezar
                _transaction?.Rollback();
                throw;
            }
        }

        public void EmpezarTransaccion(IsolationLevel nivelAislamiento = IsolationLevel.ReadCommitted)
        {
            // ReadCommitted evita leer cambios que otra transaccion todavia no ha confirmado
            _transaction = _Contexto.Database.BeginTransaction(nivelAislamiento);
        }

        public void Rollback()
        {
            _transaction?.Rollback();
        }

        public void CerrarConexion()
        {
            // cierra la conexion manualmente cuando un proceso largo ya termino de usarla
            _Contexto.Database.CloseConnection();
        }

        public void Dispose()
        {
            // libera la transaccion y el contexto para no dejar conexiones abiertas
            _transaction?.Dispose();
            _Contexto.Dispose();
        }

        #endregion
    }
}
