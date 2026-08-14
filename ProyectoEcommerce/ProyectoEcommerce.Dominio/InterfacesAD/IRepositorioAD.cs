using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using ProyectoEcommerce.Utilidades;

namespace ProyectoEcommerce.Dominio.InterfacesAD
{
    // estas son las operaciones comunes que cualquier entidad puede pedirle a la BD
    // TEntity representa la tabla que se esta trabajando en ese momento
    public interface IRepositorioAD<TEntity> where TEntity : class
    {
        // agrega un registro nuevo y devuelve la entidad ya guardada
        Task<Respuesta<TEntity>> InsertarAsync(TEntity objEntidad);

        // actualiza un registro con los valores que recibe
        Task<Respuesta<TEntity>> ModificarAsync(TEntity objEntidad);

        // hace un borrado fisico cuando la LN realmente lo necesita
        Task<Respuesta<bool>> EliminarAsync(TEntity objEntidad);

        // lista registros y puede traer relaciones con Include
        Task<Respuesta<IEnumerable<TEntity>>> ListarAsync(List<string>? objIncludes = null);

        // aplica una condicion que Entity Framework convierte a WHERE
        Task<Respuesta<IEnumerable<TEntity>>> BuscarAsync(Expression<Func<TEntity, bool>> objPredicado, List<string>? objIncludes = null);

        // filtra, acomoda y trae solo una pagina para no cargar todo en memoria
        Task<Respuesta<IEnumerable<TEntity>>> BuscarPaginadoAsync(
            Expression<Func<TEntity, bool>> objPredicado,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> ordenar,
            int omitir,
            int tomar,
            List<string>? objIncludes = null);

        // trae el primer registro que cumpla la condicion o null si no existe
        Task<Respuesta<TEntity>> ObtenerEntidadAsync(Expression<Func<TEntity, bool>> objPredicado, List<string>? objIncludes = null);

        // cuenta en SQL los registros que cumplen la condicion
        Task<Respuesta<int?>> ContarAsync(Expression<Func<TEntity, bool>> objPredicado, List<string>? objIncludes = null);
    }
}

