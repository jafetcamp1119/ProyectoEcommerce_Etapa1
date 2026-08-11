import { ICarritoActual } from './ICarrito';

/** Datos del Cliente utilizados para preparar la entrega. */
export interface ICheckoutCliente {
  nombreCompleto: string;
  correo: string;
  telefono: string;
  direccion: string | null;
}

export interface ICheckoutPreparacion {
  cliente: ICheckoutCliente;
  carrito: ICarritoActual;
}

/** Información que Angular confirma; los importes y UsuarioId no forman parte de la solicitud. */
export interface IConfirmarCompraSolicitud {
  correoDestino: string;
  direccionEnvio: string;
  metodoPago: 'TARJETA' | 'EFECTIVO';
  correoConfirmado: boolean;
}

export interface ICompraCompletada {
  ordenId: number;
  numeroOrden: string;
  total: number;
  correoDestino: string;
  facturaGenerada: boolean;
  correoEnviado: boolean;
  mensaje: string;
}
