using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using ProyectoEcommerce.Dominio.InterfacesAD;
using ProyectoEcommerce.Utilidades;

namespace ProyectoEcommerce.AccesoDatos.Implementaciones
{
    // este repositorio tiene las operaciones comunes de la BD
    // TEntity cambia por FamiliaProducto, Categoria, Producto o la entidad que se necesite
    public class RepositorioAD<TEntity> : IRepositorioAD<TEntity> where TEntity : class
    {

        #region Atributos y Variables


        protected readonly DbContext _context;


        public RepositorioAD(DbContext context)

        {

            // recibe el mismo contexto que guarda la unidad de trabajo
            this._context = context;

        }



        #endregion#region Métodos Públicos


        // recibe una entidad nueva, la agrega al contexto y devuelve la misma entidad con su ID
        public async Task<Respuesta<TEntity>> InsertarAsync(TEntity objEntidad)

        {

            Respuesta<TEntity> objRespuesta = new Respuesta<TEntity>();

            try
            {

                // Set escoge la tabla que corresponde al tipo TEntity
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


        // marca toda la entidad como modificada y guarda sus valores actuales en la BD
        public async Task<Respuesta<TEntity>> ModificarAsync(TEntity objEntidad)

        {

            Respuesta<TEntity> objRespuesta = new Respuesta<TEntity>();

            try
            {

                // Update le avisa a Entity Framework que debe crear un UPDATE para esta entidad
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


        // borra fisicamente una entidad, aunque varias LN usan desactivacion en vez de llamar este metodo
        public async Task<Respuesta<bool>> EliminarAsync(TEntity objEntidad)

        {

            Respuesta<bool> objRespuesta = new Respuesta<bool>();

            try
            {

                // este estado hace que SaveChanges mande un DELETE a SQL Server
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


        // trae todos los registros y opcionalmente las relaciones indicadas en objIncludes
        public async Task<Respuesta<IEnumerable<TEntity>>> ListarAsync(List<string>? objIncludes = null)

        {

            Respuesta<IEnumerable<TEntity>> objRespuesta = new Respuesta<IEnumerable<TEntity>>();

            try
            {

                // IQueryable va armando la consulta sin ejecutarla todavia
                IQueryable<TEntity> objPreconsulta = _context.Set<TEntity>();


                if (objIncludes != null)

                {

                    // Include agrega las tablas relacionadas que la LN necesita en la respuesta
                    objIncludes.ForEach(x => objPreconsulta = objPreconsulta.Include(x));

                }


                // ToListAsync ejecuta la consulta en SQL y trae todos los resultados
                objRespuesta.Data = await objPreconsulta.ToListAsync();

            }

            catch (Exception ex)

            {

                objRespuesta.Error = ex.Message;

                objRespuesta.Data = null;

            }

            return objRespuesta;

        }

        // recibe una condicion como x => x.Activo y devuelve solo los registros que la cumplen
        public async Task<Respuesta<IEnumerable<TEntity>>> 
        BuscarAsync(Expression<Func<TEntity, bool>> objPredicado, List<string>? objIncludes = null)

        {

            Respuesta<IEnumerable<TEntity>> objRespuesta = new Respuesta<IEnumerable<TEntity>>();

            try
            {

                IQueryable<TEntity> objPreconsulta = _context.Set<TEntity>();


                if (objIncludes != null)

                {

                    objIncludes.ForEach(x => objPreconsulta = objPreconsulta.Include(x));

                }


                // Where convierte el predicado recibido en el filtro WHERE de SQL
                objRespuesta.Data = await objPreconsulta.Where(objPredicado).ToListAsync();

            }

            catch (Exception ex)

            {

                objRespuesta.Error = ex.Message;

                objRespuesta.Data = null;

            }

            return objRespuesta;

        }


        // filtra y ordena en SQL, luego trae solamente el pedazo que pertenece a la pagina pedida
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

                // mientras siga como IQueryable el filtro y la paginacion se ejecutan en SQL Server
                consulta = consulta.Where(objPredicado);
                // Skip salta los registros de paginas anteriores y Take limita cuantos trae
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


        // busca un solo registro con la condicion recibida y devuelve null cuando no encuentra ninguno
        public async Task<Respuesta<TEntity>> 
        ObtenerEntidadAsync(Expression<Func<TEntity, bool>> objPredicado, List<string>? objIncludes = null)

        {

            Respuesta<TEntity> objRespuesta = new Respuesta<TEntity>();

            try
            {

                IQueryable<TEntity> objPreconsulta = _context.Set<TEntity>();


                if (objIncludes != null)

                {

                    objIncludes.ForEach(x => objPreconsulta = objPreconsulta.Include(x));

                }


                // FirstOrDefaultAsync trae el primero y devuelve null si la consulta quedo vacia
                objRespuesta.Data = await objPreconsulta.Where(objPredicado).FirstOrDefaultAsync();

            }

            catch (Exception ex)

            {

                objRespuesta.Error = ex.Message;

                objRespuesta.Data = null;

            }

            return objRespuesta;

        }

        // cuenta en la BD cuantos registros cumplen la condicion sin traer todas las filas a memoria
        public async Task<Respuesta<int?>>
        ContarAsync(Expression<Func<TEntity, bool>> objPredicado, List<string>? objIncludes = null)
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

