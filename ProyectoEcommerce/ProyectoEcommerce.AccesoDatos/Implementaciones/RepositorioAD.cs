using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using ProyectoEcommerce.Dominio.InterfacesAD;
using ProyectoEcommerce.Utilidades;

namespace ProyectoEcommerce.AccesoDatos.Implementaciones
{
    /// <summary>
    /// Implementación genérica de acceso a datos para las entidades administradas por Entity Framework.
    /// </summary>
    /// <typeparam name="TEntity">Tipo de entidad persistida.</typeparam>
    public class RepositorioAD<TEntity> : IRepositorioAD<TEntity> where TEntity : class
    {

        #region Atributos y Variables


        protected readonly DbContext _context;


        public RepositorioAD(DbContext context)

        {

            this._context = context;

        }



        #endregion#region Métodos Públicos


        public async Task<Respuesta<TEntity>> InsertarAsync(TEntity objEntidad)

        {

            Respuesta<TEntity> objRespuesta = new Respuesta<TEntity>();

            try
            {

                await _context.Set<TEntity>().AddAsync(objEntidad);

                await _context.SaveChangesAsync();

                objRespuesta.Data = objEntidad;

            }

            catch (Exception ex)

            {

                objRespuesta.Error = ex.Message;

                objRespuesta.Data = null;

            }

            return objRespuesta;

        }


        public async Task<Respuesta<TEntity>> ModificarAsync(TEntity objEntidad)

        {

            Respuesta<TEntity> objRespuesta = new Respuesta<TEntity>();

            try
            {

                _context.Set<TEntity>().Update(objEntidad);

                await _context.SaveChangesAsync();

                objRespuesta.Data = objEntidad;

            }

            catch (Exception ex)

            {

                objRespuesta.Error = ex.Message;

                objRespuesta.Data = null;

            }

            return objRespuesta;

        }


        public async Task<Respuesta<bool>> EliminarAsync(TEntity objEntidad)

        {

            Respuesta<bool> objRespuesta = new Respuesta<bool>();

            try
            {

                _context.Entry(objEntidad).State = EntityState.Deleted;

                await _context.SaveChangesAsync();

                objRespuesta.Data = true;

            }

            catch (Exception ex)

            {

                objRespuesta.Error = ex.Message;

                objRespuesta.Data = false;

            }

            return objRespuesta;

        }


        public async Task<Respuesta<IEnumerable<TEntity>>> ListarAsync(List<string>? objIncludes = null)

        {

            Respuesta<IEnumerable<TEntity>> objRespuesta = new Respuesta<IEnumerable<TEntity>>();

            try
            {

                IQueryable<TEntity> objPreconsulta = _context.Set<TEntity>();


                if (objIncludes != null)

                {

                    objIncludes.ForEach(x => objPreconsulta = objPreconsulta.Include(x));

                }


                objRespuesta.Data = await objPreconsulta.ToListAsync();

            }

            catch (Exception ex)

            {

                objRespuesta.Error = ex.Message;

                objRespuesta.Data = null;

            }

            return objRespuesta;

        }

        public async Task<Respuesta<IEnumerable<TEntity>>> BuscarAsync(Expression<Func<TEntity, bool>> objPredicado, List<string>? objIncludes = null)

        {

            Respuesta<IEnumerable<TEntity>> objRespuesta = new Respuesta<IEnumerable<TEntity>>();

            try
            {

                IQueryable<TEntity> objPreconsulta = _context.Set<TEntity>();


                if (objIncludes != null)

                {

                    objIncludes.ForEach(x => objPreconsulta = objPreconsulta.Include(x));

                }


                objRespuesta.Data = await objPreconsulta.Where(objPredicado).ToListAsync();

            }

            catch (Exception ex)

            {

                objRespuesta.Error = ex.Message;

                objRespuesta.Data = null;

            }

            return objRespuesta;

        }


        public async Task<Respuesta<IEnumerable<TEntity>>> BuscarPaginadoAsync(
            Expression<Func<TEntity, bool>> objPredicado,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> ordenar,
            int omitir,
            int tomar,
            List<string>? objIncludes = null)
        {
            var respuesta = new Respuesta<IEnumerable<TEntity>>();
            try
            {
                IQueryable<TEntity> consulta = _context.Set<TEntity>();
                if (objIncludes != null)
                {
                    objIncludes.ForEach(x => consulta = consulta.Include(x));
                }

                // El filtro, orden y paginación se mantienen en IQueryable para ejecutarse en SQL Server.
                consulta = consulta.Where(objPredicado);
                respuesta.Data = await ordenar(consulta)
                    .Skip(Math.Max(0, omitir))
                    .Take(Math.Max(1, tomar))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                respuesta.Success = false;
                respuesta.Error = ex.Message;
            }
            return respuesta;
        }


        public async Task<Respuesta<TEntity>> ObtenerEntidadAsync(Expression<Func<TEntity, bool>> objPredicado, List<string>? objIncludes = null)

        {

            Respuesta<TEntity> objRespuesta = new Respuesta<TEntity>();

            try
            {

                IQueryable<TEntity> objPreconsulta = _context.Set<TEntity>();


                if (objIncludes != null)

                {

                    objIncludes.ForEach(x => objPreconsulta = objPreconsulta.Include(x));

                }


                objRespuesta.Data = await objPreconsulta.Where(objPredicado).FirstOrDefaultAsync();

            }

            catch (Exception ex)

            {

                objRespuesta.Error = ex.Message;

                objRespuesta.Data = null;

            }

            return objRespuesta;

        }

        public async Task<Respuesta<int?>> ContarAsync(Expression<Func<TEntity, bool>> objPredicado, List<string>? objIncludes = null)
        {
            var respuesta = new Respuesta<int?>();
            try
            {
                IQueryable<TEntity> consulta = _context.Set<TEntity>();
                if (objIncludes != null)
                {
                    objIncludes.ForEach(x => consulta = consulta.Include(x));
                }
                respuesta.Data = await consulta.CountAsync(objPredicado);
            }
            catch (Exception ex)
            {
                respuesta.Success = false;
                respuesta.Error = ex.Message;
            }
            return respuesta;
        }
    }
}

