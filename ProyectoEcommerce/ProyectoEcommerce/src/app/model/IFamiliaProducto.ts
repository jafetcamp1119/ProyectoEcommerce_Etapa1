/** Familia utilizada en mantenimiento y navegación del catálogo. */
export interface IFamiliaProducto {
  familiaId: number;
  nombre: string;
  descripcion: string | null;
  urlImagen: string | null;
  activo: boolean;
}
