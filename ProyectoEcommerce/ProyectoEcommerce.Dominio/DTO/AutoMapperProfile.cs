using AutoMapper;
using ProyectoEcommerce.Dominio.Entidades;
using ProyectoEcommerce.Dominio.EntidadesTipadas;

namespace ProyectoEcommerce.Dominio.DTO
{
    /// <summary>
    /// Define la conversión entre entidades Database First y objetos tipados que viajan por la API.
    /// </summary>
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<TFamiliaProducto, FamiliaProducto>().ReverseMap();
            CreateMap<TCategoria, Categoria>().ReverseMap();
            CreateMap<TImpuesto, Impuesto>().ReverseMap();
            // En escritura se ignoran identificadores, fechas y navegaciones controladas por EF o por la LN.
            CreateMap<TProducto, Producto>()
                .ForMember(x => x.ProductoId, o => o.Ignore())
                .ForMember(x => x.FechaCreacion, o => o.Ignore())
                .ForMember(x => x.Categoria, o => o.Ignore())
                .ForMember(x => x.Impuesto, o => o.Ignore())
                .ForMember(x => x.Imagenes, o => o.Ignore())
                .ForMember(x => x.OrdenDetalles, o => o.Ignore());
            CreateMap<Producto, TProducto>()
                .ForMember(x => x.FamiliaId, o => o.MapFrom(x => x.Categoria.FamiliaId))
                .ForMember(x => x.FamiliaNombre, o => o.MapFrom(x => x.Categoria.Familia.Nombre))
                .ForMember(x => x.CategoriaNombre, o => o.MapFrom(x => x.Categoria.Nombre))
                .ForMember(x => x.ImpuestoNombre, o => o.MapFrom(x => x.Impuesto.Nombre))
                .ForMember(x => x.ImpuestoPorcentaje, o => o.MapFrom(x => x.Impuesto.Porcentaje))
                .ForMember(x => x.Disponible, o => o.MapFrom(x => x.Stock > 0))
                .ForMember(x => x.EstadoStock, o => o.MapFrom(x => x.Stock == 0 ? "Agotado" : x.Stock <= x.StockMinimo ? "Stock bajo" : "Disponible"))
                .ForMember(x => x.ImagenPrincipal, o => o.MapFrom(x => x.Imagenes
                    .Where(i => i.Activo)
                    .OrderByDescending(i => i.EsPrincipal)
                    .ThenBy(i => i.Orden)
                    .ThenBy(i => i.ImagenId)
                    .FirstOrDefault()));
            // El catálogo reúne nombres relacionados, disponibilidad e imagen principal en una sola respuesta.
            CreateMap<Producto, TProductoCatalogo>()
                .ForMember(x => x.FamiliaId, o => o.MapFrom(x => x.Categoria.FamiliaId))
                .ForMember(x => x.FamiliaNombre, o => o.MapFrom(x => x.Categoria.Familia.Nombre))
                .ForMember(x => x.CategoriaNombre, o => o.MapFrom(x => x.Categoria.Nombre))
                .ForMember(x => x.ImpuestoNombre, o => o.MapFrom(x => x.Impuesto.Nombre))
                .ForMember(x => x.ImpuestoPorcentaje, o => o.MapFrom(x => x.Impuesto.Porcentaje))
                .ForMember(x => x.Disponible, o => o.MapFrom(x => x.Stock > 0))
                .ForMember(x => x.EstadoStock, o => o.MapFrom(x => x.Stock == 0 ? "Agotado" : x.Stock <= x.StockMinimo ? "Stock bajo" : "Disponible"))
                .ForMember(x => x.ImagenPrincipal, o => o.MapFrom(x => x.Imagenes
                    .Where(i => i.Activo)
                    .OrderByDescending(i => i.EsPrincipal)
                    .ThenBy(i => i.Orden)
                    .ThenBy(i => i.ImagenId)
                    .FirstOrDefault()));
            CreateMap<TUsuario, Usuario>().ReverseMap();
            CreateMap<TOrden, Orden>().ReverseMap();
            CreateMap<TOrdenDetalle, OrdenDetalle>().ReverseMap();
            CreateMap<TProductoImagen, ProductoImagen>().ReverseMap();
            CreateMap<TDescuento, Descuento>()
                .ForMember(x => x.DescuentoId, o => o.Ignore())
                .ForMember(x => x.MontoFijo, o => o.Ignore())
                .ForMember(x => x.Producto, o => o.Ignore())
                .ForMember(x => x.Categoria, o => o.Ignore())
                .ForMember(x => x.Familia, o => o.Ignore());
        }
    }
}
