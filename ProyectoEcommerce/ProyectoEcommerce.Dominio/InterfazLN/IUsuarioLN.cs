using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Utilidades;

namespace ProyectoEcommerce.Dominio.InterfazLN
{
    // aqui quedan las acciones de usuarios que los controllers pueden pedirle a la LN
    public interface IUsuarioLN
    {
        // estas operaciones se conservan porque forman parte del patron comun del proyecto
        Task<Respuesta<TUsuario>> InsertarAsync(TUsuario datos);
        Task<Respuesta<TUsuario>> ModificarAsync(TUsuario datos);
        Task<Respuesta<bool>> EliminarAsync(TUsuario datos);
        Task<Respuesta<IEnumerable<TUsuario>>> BuscarAsync(TUsuario datos);
        Task<Respuesta<TUsuario>> ObtenerAsync(TUsuario datos);
        Task<Respuesta<IEnumerable<TUsuario>>> ListarAsync();
        // registra un Cliente y guarda un hash, nunca la contraseña escrita
        Task<Respuesta<TUsuario>> RegistrarAsync(TRegistroUsuario datos);
        // revisa si todavia hace falta crear el primer Administrador
        Task<Respuesta<bool>> RequiereConfiguracionInicialAsync();
        // recibe los datos iniciales y crea el primer Administrador dentro de una transaccion
        Task<Respuesta<TUsuario>> CrearAdministradorInicialAsync(TRegistroUsuario datos);
        // revisa correo, contraseña, bloqueos y estado antes de dejar que el controller cree el JWT
        Task<Respuesta<TEstadoAutenticacion>> AutenticarAsync(TLoginUsuario datos);
        // trae una pagina de usuarios usando los filtros de administracion
        Task<Respuesta<TPagina<TUsuario>>> ListarAdministracionAsync(TFiltroUsuarios filtro);
        // devuelve los roles que todavia se pueden asignar
        Task<Respuesta<IEnumerable<TRol>>> ListarRolesAsync();
        // cambia el rol y deja en bitacora cual Administrador lo hizo
        Task<Respuesta<TUsuario>> CambiarRolAsync(TCambioRolUsuario datos, int administradorId);
        // activa o desactiva una cuenta cuidando que el Administrador no se bloquee solo
        Task<Respuesta<TUsuario>> CambiarEstadoAsync(TCambioEstadoUsuario datos, int administradorId);
    }
}
