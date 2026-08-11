using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using ProyectoEcommerce.Utilidades;

namespace ProyectoEcommerce.Dominio.InterfacesAD
{
    /// <summary>
    /// Define operaciones genéricas de persistencia usadas por las entidades de Entity Framework.
    /// </summary>
    /// <typeparam name="TEntity">Entidad Database First que administra el repositorio.</typeparam>
    public interface IRepositorioAD<TEntity> where TEntity : class
    {
        /// <summary>Agrega una entidad al contexto.</summary>
        Task<Respuesta<TEntity>> InsertarAsync(TEntity objEntidad);

        /// <summary>Marca una entidad existente como modificada.</summary>
        Task<Respuesta<TEntity>> ModificarAsync(TEntity objEntidad);

        /// <summary>Elimina la entidad recibida cuando la LN solicita una eliminación física.</summary>
        Task<Respuesta<bool>> EliminarAsync(TEntity objEntidad);

        /// <summary>Lista entidades e incluye relaciones opcionales.</summary>
        Task<Respuesta<IEnumerable<TEntity>>> ListarAsync(List<string>? objIncludes = null);

        /// <summary>Busca entidades mediante una expresión ejecutada por Entity Framework.</summary>
        Task<Respuesta<IEnumerable<TEntity>>> BuscarAsync(Expression<Func<TEntity, bool>> objPredicado, List<string>? objIncludes = null);

        /// <summary>Ejecuta búsqueda, ordenamiento y paginación en la base de datos.</summary>
        Task<Respuesta<IEnumerable<TEntity>>> BuscarPaginadoAsync(
            Expression<Func<TEntity, bool>> objPredicado,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> ordenar,
            int omitir,
            int tomar,
            List<string>? objIncludes = null);

        /// <summary>Obtiene la primera entidad que satisface el predicado.</summary>
        Task<Respuesta<TEntity>> ObtenerEntidadAsync(Expression<Func<TEntity, bool>> objPredicado, List<string>? objIncludes = null);

        /// <summary>Cuenta registros que cumplen el filtro.</summary>
        Task<Respuesta<int?>> ContarAsync(Expression<Func<TEntity, bool>> objPredicado, List<string>? objIncludes = null);
    }
}

