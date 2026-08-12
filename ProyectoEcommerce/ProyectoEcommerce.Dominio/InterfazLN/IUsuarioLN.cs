using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Utilidades;

namespace ProyectoEcommerce.Dominio.InterfazLN
{
    /// <summary>Define registro, autenticación y administración de usuarios y roles.</summary>
    public interface IUsuarioLN
    {
        Task<Respuesta<TUsuario>> InsertarAsync(TUsuario datos);
        Task<Respuesta<TUsuario>> ModificarAsync(TUsuario datos);
        Task<Respuesta<bool>> EliminarAsync(TUsuario datos);
        Task<Respuesta<IEnumerable<TUsuario>>> BuscarAsync(TUsuario datos);
        Task<Respuesta<TUsuario>> ObtenerAsync(TUsuario datos);
        Task<Respuesta<IEnumerable<TUsuario>>> ListarAsync();
        /// <summary>Registra una cuenta de Cliente almacenando la contraseña mediante hash.</summary>
        Task<Respuesta<TUsuario>> RegistrarAsync(TRegistroUsuario datos);
        /// <summary>Indica si todavía no existe un Administrador activo.</summary>
        Task<Respuesta<bool>> RequiereConfiguracionInicialAsync();
        /// <summary>Crea de forma atómica el primer Administrador autorizado.</summary>
        Task<Respuesta<TUsuario>> CrearAdministradorInicialAsync(TRegistroUsuario datos);
        /// <summary>Valida credenciales, bloqueos y estado antes de permitir crear un JWT.</summary>
        Task<Respuesta<TEstadoAutenticacion>> AutenticarAsync(TLoginUsuario datos);
        /// <summary>Lista usuarios con filtros y paginación administrativa.</summary>
        Task<Respuesta<TPagina<TUsuario>>> ListarAdministracionAsync(TFiltroUsuarios filtro);
        /// <summary>Lista los roles activos que pueden asignarse.</summary>
        Task<Respuesta<IEnumerable<TRol>>> ListarRolesAsync();
        /// <summary>Cambia el rol y registra al Administrador que realizó la acción.</summary>
        Task<Respuesta<TUsuario>> CambiarRolAsync(TCambioRolUsuario datos, int administradorId);
        /// <summary>Activa o desactiva una cuenta sin permitir que el Administrador se desactive a sí mismo.</summary>
        Task<Respuesta<TUsuario>> CambiarEstadoAsync(TCambioEstadoUsuario datos, int administradorId);
    }
}
