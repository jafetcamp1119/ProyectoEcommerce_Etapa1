export interface IProductoImagen {
  imagenId: number;
  productoId: number;
  urlImagen: string;
  textoAlternativo: string | null;
  esPrincipal: boolean;
  orden: number;
  activo: boolean;
}
