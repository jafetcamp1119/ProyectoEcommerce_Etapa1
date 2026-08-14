// forma basica que conservan las operaciones administrativas originales de ordenes
export interface IOrden {
  ordenId: number;
  usuarioId: number;
  fechaOrden: string;
  estado: string;
  tipoOrden: string;
  direccionEnvio: string | null;
  moneda: string;
  total: number | null;
}

export interface IFiltroOrdenes {
  numero?: string;
  cliente?: string;
  estado?: string;
  fechaDesde?: string;
  fechaHasta?: string;
  pagina?: number;
  tamanoPagina?: 25 | 50 | 75 | 100;
}

export interface IPaginaOrdenes {
  items: IOrdenResumen[];
  pagina: number;
  tamanoPagina: number;
  total: number;
}

/** Resumen de orden mostrado en los listados de Cliente y Administrador. */
export interface IOrdenResumen {
  ordenId: number;
  numeroOrden: string;
  fechaOrden: string;
  estado: string;
  cantidadProductos: number;
  total: number;
  metodoPago: string | null;
  cliente: string;
  correo: string;
  facturaDisponible: boolean;
}

/** Detalle de una orden con productos y desglose histórico. */
export interface IOrdenDetalleConsulta extends IOrdenResumen {
  direccionEnvio: string;
  subtotal: number;
  impuestos: number;
  descuentos: number;
  numeroFactura: string | null;
  correoEnviado: boolean;
  productos: IOrdenProductoConsulta[];
}

export interface IOrdenProductoConsulta {
  productoId: number;
  nombre: string;
  cantidad: number;
  precioUnitario: number;
  porcentajeImpuesto: number;
  porcentajeDescuento: number;
  subtotal: number;
  impuestos: number;
  descuentos: number;
  totalLinea: number;
}
