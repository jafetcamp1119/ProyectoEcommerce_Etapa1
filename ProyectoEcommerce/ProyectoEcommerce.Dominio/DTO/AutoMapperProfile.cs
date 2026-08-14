using AutoMapper;
using ProyectoEcommerce.Dominio.Entidades;
using ProyectoEcommerce.Dominio.EntidadesTipadas;

namespace ProyectoEcommerce.Dominio.DTO
{
    // aqui se le dice a AutoMapper como convertir entre las entidades de la BD y los datos de la API
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // ReverseMap deja convertir en los dos sentidos
            // UrlImagen pasa solo porque tiene el mismo nombre y tipo en la entidad y en el DTO
            CreateMap<TFamiliaProducto, FamiliaProducto>().ReverseMap();
            CreateMap<TCategoria, Categoria>().ReverseMap();
            CreateMap<TImpuesto, Impuesto>().ReverseMap();
            CreateMap<TProveedor, Proveedor>()
                .ForMember(x => x.ProveedorId, o => o.Ignore())
                .ForMember(x => x.FechaRegistro, o => o.Ignore())
                .ForMember(x => x.Familias, o => o.Ignore())
                .ForMember(x => x.Categorias, o => o.Ignore())
                .ForMember(x => x.Productos, o => o.Ignore())
                .ForMember(x => x.Compras, o => o.Ignore());
            CreateMap<Proveedor, TProveedor>();

            // al guardar un producto ignora datos que controla la BD, Entity Framework o la LN
            CreateMap<TProducto, Producto>()
                .ForMember(x => x.ProductoId, o => o.Ignore())
                .ForMember(x => x.FechaCreacion, o => o.Ignore())
                .ForMember(x => x.Categoria, o => o.Ignore())
                .ForMember(x => x.Impuesto, o => o.Ignore())
                .ForMember(x => x.Imagenes, o => o.Ignore())
                .ForMember(x => x.OrdenDetalles, o => o.Ignore());
            // al devolver un producto agarra tambien nombres de sus relaciones y calcula su estado de stock
            CreateMap<Producto, TProducto>()
                .ForMember(x => x.FamiliaId, o => o.MapFrom(x => x.Categoria.FamiliaId))
                .ForMember(x => x.FamiliaNombre, o => o.MapFrom(x => x.Categoria.Familia.Nombre))
                .ForMember(x => x.CategoriaNombre, o => o.MapFrom(x => x.Categoria.Nombre))
                .ForMember(x => x.ImpuestoNombre, o => o.MapFrom(x => x.Impuesto.Nombre))
                .ForMember(x => x.ImpuestoPorcentaje, o => o.MapFrom(x => x.Impuesto.Porcentaje))
                .ForMember(x => x.Disponible, o => o.MapFrom(x => x.Stock > 0))
                .ForMember(x => x.EstadoStock, o => o.MapFrom(x => x.Stock == 0 ? "Agotado" : x.Stock <= x.StockMinimo ? "Stock bajo" : "Disponible"))
                // Where deja imagenes activas, los OrderBy las acomodan y FirstOrDefault toma la principal
                .ForMember(x => x.ImagenPrincipal, o => o.MapFrom(x => x.Imagenes
                    .Where(i => i.Activo)
                    .OrderByDescending(i => i.EsPrincipal)
                    .ThenBy(i => i.Orden)
                    .ThenBy(i => i.ImagenId)
                    .FirstOrDefault()));
            // esta version es la que usa el catalogo para mandar todo lo necesario en una sola respuesta
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
            // estos tipos tienen propiedades con los mismos nombres y no necesitan reglas extra
            CreateMap<TUsuario, Usuario>().ReverseMap();
            CreateMap<TOrden, Orden>().ReverseMap();
            CreateMap<TOrdenDetalle, OrdenDetalle>().ReverseMap();
            CreateMap<TProductoImagen, ProductoImagen>().ReverseMap();
            // en descuentos la LN decide el ID y las relaciones para evitar que lleguen objetos falsos desde Angular
            CreateMap<TDescuento, Descuento>()
                .ForMember(x => x.DescuentoId, o => o.Ignore())
                .ForMember(x => x.MontoFijo, o => o.Ignore())
                .ForMember(x => x.Producto, o => o.Ignore())
                .ForMember(x => x.Categoria, o => o.Ignore())
                .ForMember(x => x.Familia, o => o.Ignore());
        }
    }
}
