import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { IRespuesta } from '../model/IAuth';
import { ICatalogosDescuento, IDescuento, IDescuentoAplicado } from '../model/IDescuento';

@Injectable({ providedIn: 'root' })
export class DescuentoService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.baseUrl}/Descuento`;

  listar(): Observable<IRespuesta<IDescuento[]>> {
    return this.http.get<IRespuesta<IDescuento[]>>(`${this.url}/Listar`);
  }

  obtener(descuentoId: number): Observable<IRespuesta<IDescuento>> {
    return this.http.get<IRespuesta<IDescuento>>(`${this.url}/Obtener/${descuentoId}`);
  }

  catalogos(): Observable<IRespuesta<ICatalogosDescuento>> {
    return this.http.get<IRespuesta<ICatalogosDescuento>>(`${this.url}/Catalogos`);
  }

  insertar(datos: IDescuento): Observable<IRespuesta<IDescuento>> {
    return this.http.post<IRespuesta<IDescuento>>(`${this.url}/Insertar`, datos);
  }

  modificar(datos: IDescuento): Observable<IRespuesta<IDescuento>> {
    return this.http.put<IRespuesta<IDescuento>>(`${this.url}/Modificar`, datos);
  }

  cambiarEstado(descuentoId: number, activo: boolean): Observable<IRespuesta<IDescuento>> {
    return this.http.put<IRespuesta<IDescuento>>(`${this.url}/Estado/${descuentoId}`, { activo });
  }

  aplicadoProducto(productoId: number): Observable<IRespuesta<IDescuentoAplicado>> {
    return this.http.get<IRespuesta<IDescuentoAplicado>>(`${this.url}/AplicadoProducto/${productoId}`);
  }
}
