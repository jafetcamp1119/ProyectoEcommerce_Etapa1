import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { IRespuesta } from '../model/IAuth';
import { ICarritoActual, IResultadoAgregarCarrito } from '../model/ICarrito';

/**
 * Comunica las acciones del carrito con la API y mantiene el indicador global de cantidades.
 */
@Injectable({ providedIn: 'root' })
export class CarritoService {
  private readonly http = inject(HttpClient);
  private readonly cantidadSignal = signal(0);
  readonly cantidadTotal = this.cantidadSignal.asReadonly();

  agregar(productoId: number, cantidad = 1): Observable<IRespuesta<IResultadoAgregarCarrito>> {
    return this.http.post<IRespuesta<IResultadoAgregarCarrito>>(
      `${environment.baseUrl}/Carrito/Agregar`,
      { productoId, cantidad }
    ).pipe(tap(respuesta => this.cantidadSignal.set(respuesta.data?.cantidadTotal ?? this.cantidadSignal())));
  }

  obtenerActual(): Observable<IRespuesta<ICarritoActual>> {
    return this.http.get<IRespuesta<ICarritoActual>>(`${environment.baseUrl}/Carrito/Actual`)
      .pipe(tap(respuesta => this.cantidadSignal.set(respuesta.data?.cantidadTotal ?? 0)));
  }

  actualizarCantidad(carritoDetalleId: number, cantidad: number): Observable<IRespuesta<ICarritoActual>> {
    return this.http.put<IRespuesta<ICarritoActual>>(
      `${environment.baseUrl}/Carrito/Cantidad`,
      { carritoDetalleId, cantidad }
    ).pipe(tap(respuesta => this.cantidadSignal.set(respuesta.data?.cantidadTotal ?? 0)));
  }

  eliminar(carritoDetalleId: number): Observable<IRespuesta<ICarritoActual>> {
    return this.http.delete<IRespuesta<ICarritoActual>>(
      `${environment.baseUrl}/Carrito/Detalle/${carritoDetalleId}`
    ).pipe(tap(respuesta => this.cantidadSignal.set(respuesta.data?.cantidadTotal ?? 0)));
  }

  reiniciarIndicador(): void { this.cantidadSignal.set(0); }
}
