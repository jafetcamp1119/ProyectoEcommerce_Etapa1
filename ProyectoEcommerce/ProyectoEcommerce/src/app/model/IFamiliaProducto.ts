/** Familia utilizada en mantenimiento y navegación del catálogo. */
export interface IFamiliaProducto {
  familiaId: number;
  nombre: string;
  descripcion: string | null;
  activo: boolean;
}
