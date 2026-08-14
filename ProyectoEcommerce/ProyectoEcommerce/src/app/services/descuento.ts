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

  // trae todos los descuentos para administracion
  listar(): Observable<IRespuesta<IDescuento[]>> {
    return this.http.get<IRespuesta<IDescuento[]>>(`${this.url}/Listar`);
  }

  // trae un descuento especifico por ID
  obtener(descuentoId: number): Observable<IRespuesta<IDescuento>> {
    return this.http.get<IRespuesta<IDescuento>>(`${this.url}/Obtener/${descuentoId}`);
  }

  // pide los destinos que llenan los select dependientes del formulario
  catalogos(): Observable<IRespuesta<ICatalogosDescuento>> {
    return this.http.get<IRespuesta<ICatalogosDescuento>>(`${this.url}/Catalogos`);
  }

  // manda un descuento nuevo a la API
  insertar(datos: IDescuento): Observable<IRespuesta<IDescuento>> {
    return this.http.post<IRespuesta<IDescuento>>(`${this.url}/Insertar`, datos);
  }

  // guarda los cambios de un descuento
  modificar(datos: IDescuento): Observable<IRespuesta<IDescuento>> {
    return this.http.put<IRespuesta<IDescuento>>(`${this.url}/Modificar`, datos);
  }

  // cambia el estado logico sin borrarlo
  cambiarEstado(descuentoId: number, activo: boolean): Observable<IRespuesta<IDescuento>> {
    return this.http.put<IRespuesta<IDescuento>>(`${this.url}/Estado/${descuentoId}`, { activo });
  }

  // consulta el descuento que la API eligio como mejor para un producto
  aplicadoProducto(productoId: number): Observable<IRespuesta<IDescuentoAplicado>> {
    return this.http.get<IRespuesta<IDescuentoAplicado>>(`${this.url}/AplicadoProducto/${productoId}`);
  }
}
