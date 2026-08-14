// impuesto que usan los productos y el formulario administrativo
export interface IImpuesto {
  impuestoId: number;
  nombre: string;
  porcentaje: number;
  fechaInicio: string;
  fechaFin: string | null;
  activo: boolean;
}
