/** Resultado de agregar o acumular un producto en el carrito. */
export interface IResultadoAgregarCarrito {
  carritoId: number;
  productoId: number;
  cantidadProducto: number;
  cantidadTotal: number;
  productoNuevo: boolean;
}

/** Carrito vigente con totales calculados por el servidor. */
export interface ICarritoActual {
  carritoId: number;
  estado: string;
  cantidadTotal: number;
  subtotal: number;
  impuestos: number;
  descuentos: number;
  total: number;
  items: ICarritoItem[];
}

export interface ICarritoItem {
  carritoDetalleId: number;
  productoId: number;
  nombre: string;
  cantidad: number;
  stockDisponible: number;
  precioOriginal: number;
  precioUnitario: number;
  porcentajeImpuesto: number;
  porcentajeDescuento: number;
  tipoDescuento: string | null;
  nombreDescuento: string | null;
  subtotal: number;
  impuestos: number;
  descuentos: number;
  totalLinea: number;
  productoActivo: boolean;
  stockSuficiente: boolean;
}
