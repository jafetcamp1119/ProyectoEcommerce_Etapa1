// forma que tiene una familia cuando viaja entre la API y Angular
// urlImagen puede ser null porque la tarjeta sabe mostrar un placeholder
export interface IFamiliaProducto {
  familiaId: number;
  nombre: string;
  descripcion: string | null;
  urlImagen: string | null;
  activo: boolean;
}
