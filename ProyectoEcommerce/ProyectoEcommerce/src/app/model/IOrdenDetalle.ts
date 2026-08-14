// valores historicos de una linea de orden tal como quedaron al momento de comprar
export interface IOrdenDetalle {
  ordenDetalleId: number;
  ordenId: number;
  productoId: number;
  cantidad: number;
  precioUnitario: number;
  porcentajeImpuesto: number;
  subtotal: number;
  totalLinea: number;
}
