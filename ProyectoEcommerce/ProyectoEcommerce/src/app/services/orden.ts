import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { IRespuesta } from '../model/IAuth';
import { ICheckoutPreparacion, ICompraCompletada, IConfirmarCompraSolicitud } from '../model/ICheckout';
import { IFiltroOrdenes, IOrden, IOrdenDetalleConsulta, IPaginaOrdenes } from '../model/IOrden';

import { environment } from '../../environments/environment';

const baseUrl = environment.baseUrl;

@Injectable({
  providedIn: 'root'
})
export class OrdenService {

  private http = inject(HttpClient);


  // ========================= MANTENIMIENTO DE ORDENES =========================

  listar(): Observable<any> {
    return this.http.get<any>(`${baseUrl}/Orden/Listar`);
  }

  obtener(id: number): Observable<any> {
    return this.http.get<any>(`${baseUrl}/Orden/Obtener/${id}`);
  }

  buscar(estado: string): Observable<any> {
    return this.http.get<any>(
      `${baseUrl}/Orden/Buscar`,
      { params: { estado } }
    );
  }

  insertar(datos: IOrden): Observable<any> {
    return this.http.post<any>(
      `${baseUrl}/Orden/Insertar`,
      datos
    );
  }

  modificar(datos: IOrden): Observable<any> {
    return this.http.put<any>(
      `${baseUrl}/Orden/Modificar`,
      datos
    );
  }

  eliminar(id: number): Observable<any> {
    return this.http.delete<any>(
      `${baseUrl}/Orden/Eliminar/${id}`
    );
  }


  // ========================= CHECKOUT =========================

  prepararCheckout(): Observable<IRespuesta<ICheckoutPreparacion>> {
    return this.http.get<IRespuesta<ICheckoutPreparacion>>(
      `${baseUrl}/Orden/Checkout`
    );
  }

  confirmarCompra(
    datos: IConfirmarCompraSolicitud
  ): Observable<IRespuesta<ICompraCompletada>> {

    return this.http.post<IRespuesta<ICompraCompletada>>(
      `${baseUrl}/Orden/ConfirmarCompra`,
      datos
    );
  }


  // ========================= CONSULTA DE ORDENES =========================

  // Cliente consulta solamente sus ordenes
  misOrdenes(
    filtro: IFiltroOrdenes
  ): Observable<IRespuesta<IPaginaOrdenes>> {

    return this.http.get<IRespuesta<IPaginaOrdenes>>(
      `${baseUrl}/Orden/MisOrdenes`,
      { params: this.parametros(filtro) }
    );
  }

  // Administrador puede consultar las ordenes desde mantenimiento
  administracion(
    filtro: IFiltroOrdenes
  ): Observable<IRespuesta<IPaginaOrdenes>> {

    return this.http.get<IRespuesta<IPaginaOrdenes>>(
      `${baseUrl}/Orden/Administracion`,
      { params: this.parametros(filtro) }
    );
  }

  detalle(
    id: number
  ): Observable<IRespuesta<IOrdenDetalleConsulta>> {

    return this.http.get<IRespuesta<IOrdenDetalleConsulta>>(
      `${baseUrl}/Orden/Detalle/${id}`
    );
  }

  cancelar(
    id: number
  ): Observable<IRespuesta<boolean>> {

    return this.http.put<IRespuesta<boolean>>(
      `${baseUrl}/Orden/Cancelar/${id}`,
      {}
    );
  }


  // ========================= FACTURA =========================

  // blob se usa porque la API devuelve un archivo PDF y no un objeto JSON
  descargarFactura(
    id: number
  ): Observable<Blob> {

    return this.http.get(
      `${baseUrl}/Orden/Factura/${id}`,
      { responseType: 'blob' }
    );
  }


  // ========================= FILTROS =========================

  // convierte los filtros que tienen valor en parametros para la URL
  private parametros(
    filtro: IFiltroOrdenes
  ): HttpParams {

    let parametros = new HttpParams();

    // Object.entries permite recorrer clave y valor sin escribir cada filtro por separado
    for (const [clave, valor] of Object.entries(filtro)) {

      if (
        valor !== undefined &&
        valor !== null &&
        valor !== ''
      ) {
        parametros = parametros.set(
          clave,
          String(valor)
        );
      }

    }

    return parametros;
  }

}
