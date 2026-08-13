/** Categoría vinculada a una familia de producto. */
export interface ICategoria {
  categoriaId: number;
  familiaId: number;
  nombre: string;
  descripcion: string | null;
  urlImagen: string | null;
  activo: boolean;
}
