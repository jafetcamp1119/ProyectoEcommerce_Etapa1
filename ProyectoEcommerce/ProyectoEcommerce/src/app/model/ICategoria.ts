// forma que tiene una categoria y el ID de la familia a la que pertenece
// urlImagen puede venir vacia sin romper la tarjeta
export interface ICategoria {
  categoriaId: number;
  familiaId: number;
  nombre: string;
  descripcion: string | null;
  urlImagen: string | null;
  activo: boolean;
}
