export interface ISolicitudCompraProveedorItem {
  productoProveedorCatalogoId: number;
  cantidad: number;
}

export interface ISolicitudCompraProveedor {
  proveedorId: number;
  claveConfirmacion: string;
  productos: ISolicitudCompraProveedorItem[];
}

export interface ICompraProveedorConfirmada {
  compraProveedorId: number;
  numero: string;
  total: number;
  estado: string;
  fecha: string;
  pdfDisponible: boolean;
  mensaje: string;
}

export interface IFiltroComprasProveedor {
  proveedorId?: number;
  estado?: string;
  fechaDesde?: string;
  fechaHasta?: string;
  pagina?: number;
  tamanoPagina?: 25 | 50 | 75 | 100;
}

export interface ICompraProveedorResumen {
  compraProveedorId: number;
  numero: string;
  proveedorId: number;
  proveedor: string;
  fecha: string;
  total: number;
  estado: string;
  pdfDisponible: boolean;
}

export interface ICompraProveedorProducto {
  productoProveedorCatalogoId: number;
  productoId: number;
  nombre: string;
  cantidad: number;
  precioUnitario: number;
  subtotal: number;
}

export interface ICompraProveedorDetalle extends ICompraProveedorResumen {
  correoProveedor: string | null;
  telefonoProveedor: string | null;
  direccionProveedor: string | null;
  productos: ICompraProveedorProducto[];
}

export interface IPaginaComprasProveedor {
  items: ICompraProveedorResumen[];
  pagina: number;
  tamanoPagina: 25 | 50 | 75 | 100;
  total: number;
}
