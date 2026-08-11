import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { HttpParams } from '@angular/common/http';
import { IFiltroOrdenes, IOrden, IOrdenDetalleConsulta, IPaginaOrdenes } from '../model/IOrden';
import { environment } from '../../environments/environment';
import { IRespuesta } from '../model/IAuth';
import { ICheckoutPreparacion, ICompraCompletada, IConfirmarCompraSolicitud } from '../model/ICheckout';

const baseUrl = environment.baseUrl;

/**
 * Centraliza checkout, confirmación de compra, consulta de órdenes y descarga de facturas.
 */
@Injectable({ providedIn: 'root' })
export class OrdenService {
  private http = inject(HttpClient);
  listar(): Observable<any> { return this.http.get<any>(`${baseUrl}/Orden/Listar`); }
  obtener(id: number): Observable<any> { return this.http.get<any>(`${baseUrl}/Orden/Obtener/${id}`); }
  buscar(estado: string): Observable<any> { return this.http.get<any>(`${baseUrl}/Orden/Buscar`, { params: { estado } }); }
  insertar(datos: IOrden): Observable<any> { return this.http.post<any>(`${baseUrl}/Orden/Insertar`, datos); }
  modificar(datos: IOrden): Observable<any> { return this.http.put<any>(`${baseUrl}/Orden/Modificar`, datos); }
  eliminar(id: number): Observable<any> { return this.http.delete<any>(`${baseUrl}/Orden/Eliminar/${id}`); }
  prepararCheckout(): Observable<IRespuesta<ICheckoutPreparacion>> { return this.http.get<IRespuesta<ICheckoutPreparacion>>(`${baseUrl}/Orden/Checkout`); }
  confirmarCompra(datos: IConfirmarCompraSolicitud): Observable<IRespuesta<ICompraCompletada>> { return this.http.post<IRespuesta<ICompraCompletada>>(`${baseUrl}/Orden/ConfirmarCompra`, datos); }
  misOrdenes(filtro: IFiltroOrdenes): Observable<IRespuesta<IPaginaOrdenes>> { return this.http.get<IRespuesta<IPaginaOrdenes>>(`${baseUrl}/Orden/MisOrdenes`, { params: this.parametros(filtro) }); }
  administracion(filtro: IFiltroOrdenes): Observable<IRespuesta<IPaginaOrdenes>> { return this.http.get<IRespuesta<IPaginaOrdenes>>(`${baseUrl}/Orden/Administracion`, { params: this.parametros(filtro) }); }
  detalle(id: number): Observable<IRespuesta<IOrdenDetalleConsulta>> { return this.http.get<IRespuesta<IOrdenDetalleConsulta>>(`${baseUrl}/Orden/Detalle/${id}`); }
  cancelar(id: number): Observable<IRespuesta<boolean>> { return this.http.put<IRespuesta<boolean>>(`${baseUrl}/Orden/Cancelar/${id}`, {}); }
  descargarFactura(id: number): Observable<Blob> { return this.http.get(`${baseUrl}/Orden/Factura/${id}`, { responseType: 'blob' }); }

  private parametros(filtro: IFiltroOrdenes): HttpParams {
    let parametros = new HttpParams();
    for (const [clave, valor] of Object.entries(filtro)) {
      if (valor !== undefined && valor !== null && valor !== '') parametros = parametros.set(clave, String(valor));
    }
    return parametros;
  }
}
