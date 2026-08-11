/** Configuración de impuesto utilizada por productos y formularios administrativos. */
export interface IImpuesto {
  impuestoId: number;
  nombre: string;
  porcentaje: number;
  fechaInicio: string;
  fechaFin: string | null;
  activo: boolean;
}
