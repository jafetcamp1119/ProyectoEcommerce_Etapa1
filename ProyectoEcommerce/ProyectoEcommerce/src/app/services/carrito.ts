import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { IRespuesta } from '../model/IAuth';
import { ICarritoActual, IResultadoAgregarCarrito } from '../model/ICarrito';

// manda las acciones del carrito a la API y mantiene el numero que se ve en el encabezado
@Injectable({ providedIn: 'root' })
export class CarritoService {
  private readonly http = inject(HttpClient);
  private readonly cantidadSignal = signal(0);
  // los componentes pueden leer esta signal pero solo el servicio puede cambiarla
  readonly cantidadTotal = this.cantidadSignal.asReadonly();

  // agrega una cantidad y tap actualiza el indicador sin cambiar la respuesta del observable
  agregar(productoId: number, cantidad = 1): Observable<IRespuesta<IResultadoAgregarCarrito>> {
    return this.http.post<IRespuesta<IResultadoAgregarCarrito>>(
      `${environment.baseUrl}/Carrito/Agregar`,
      { productoId, cantidad }
    ).pipe(tap(respuesta => this.cantidadSignal.set(respuesta.data?.cantidadTotal ?? this.cantidadSignal())));
  }

  // trae el carrito completo y sincroniza la cantidad global
  obtenerActual(): Observable<IRespuesta<ICarritoActual>> {
    return this.http.get<IRespuesta<ICarritoActual>>(`${environment.baseUrl}/Carrito/Actual`)
      .pipe(tap(respuesta => this.cantidadSignal.set(respuesta.data?.cantidadTotal ?? 0)));
  }

  // manda la nueva cantidad de una linea y recibe todos los totales recalculados
  actualizarCantidad(carritoDetalleId: number, cantidad: number): Observable<IRespuesta<ICarritoActual>> {
    return this.http.put<IRespuesta<ICarritoActual>>(
      `${environment.baseUrl}/Carrito/Cantidad`,
      { carritoDetalleId, cantidad }
    ).pipe(tap(respuesta => this.cantidadSignal.set(respuesta.data?.cantidadTotal ?? 0)));
  }

  // quita una linea y vuelve a actualizar el indicador del encabezado
  eliminar(carritoDetalleId: number): Observable<IRespuesta<ICarritoActual>> {
    return this.http.delete<IRespuesta<ICarritoActual>>(
      `${environment.baseUrl}/Carrito/Detalle/${carritoDetalleId}`
    ).pipe(tap(respuesta => this.cantidadSignal.set(respuesta.data?.cantidadTotal ?? 0)));
  }

  // se usa al cerrar sesion o terminar la compra para dejar el contador en cero
  reiniciarIndicador(): void { this.cantidadSignal.set(0); }
}
