import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  ICatalogosProducto,
  IFiltroProductos,
  IPaginaProductos,
  IProducto,
  IProductoCatalogo,
  IProductoGuardar
} from '../model/IProducto';
import { environment } from '../../environments/environment';
import { IRespuesta } from '../model/IAuth';

const baseUrl = `${environment.baseUrl}/Producto`;

/**
 * Consume el catálogo filtrado y las operaciones administrativas de productos.
 */
@Injectable({ providedIn: 'root' })
export class ProductoService {
  private readonly http = inject(HttpClient);

  catalogo(filtro: IFiltroProductos = {}): Observable<IRespuesta<IPaginaProductos<IProductoCatalogo>>> {
    return this.http.get<IRespuesta<IPaginaProductos<IProductoCatalogo>>>(`${baseUrl}/Catalogo`, {
      params: this.parametros(filtro)
    });
  }

  administracion(filtro: IFiltroProductos = {}): Observable<IRespuesta<IPaginaProductos<IProducto>>> {
    return this.http.get<IRespuesta<IPaginaProductos<IProducto>>>(`${baseUrl}/Administracion`, {
      params: this.parametros(filtro)
    });
  }

  detalle(id: number): Observable<IRespuesta<IProductoCatalogo>> {
    return this.http.get<IRespuesta<IProductoCatalogo>>(`${baseUrl}/Detalle/${id}`);
  }

  detalleAdministracion(id: number): Observable<IRespuesta<IProducto>> {
    return this.http.get<IRespuesta<IProducto>>(`${baseUrl}/DetalleAdministracion/${id}`);
  }

  catalogos(): Observable<IRespuesta<ICatalogosProducto>> {
    return this.http.get<IRespuesta<ICatalogosProducto>>(`${baseUrl}/Catalogos`);
  }

  insertar(datos: IProductoGuardar): Observable<IRespuesta<IProducto>> {
    return this.http.post<IRespuesta<IProducto>>(`${baseUrl}/Insertar`, datos);
  }

  modificar(datos: IProductoGuardar): Observable<IRespuesta<IProducto>> {
    return this.http.put<IRespuesta<IProducto>>(`${baseUrl}/Modificar`, datos);
  }

  cambiarEstado(id: number, activo: boolean): Observable<IRespuesta<boolean>> {
    return this.http.put<IRespuesta<boolean>>(`${baseUrl}/CambiarEstado/${id}`, { activo });
  }

  private parametros(filtro: IFiltroProductos): HttpParams {
    let params = new HttpParams();
    for (const [clave, valor] of Object.entries(filtro)) {
      if (valor !== undefined && valor !== null && valor !== '') params = params.set(clave, String(valor));
    }
    return params;
  }
}
