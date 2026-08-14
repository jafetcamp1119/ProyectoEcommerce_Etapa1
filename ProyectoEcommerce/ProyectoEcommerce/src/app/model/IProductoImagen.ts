// datos de una imagen de producto, este modelo sigue separado de UrlImagen de familias y categorias
export interface IProductoImagen {
  imagenId: number;
  productoId: number;
  urlImagen: string;
  textoAlternativo: string | null;
  esPrincipal: boolean;
  orden: number;
  activo: boolean;
}
