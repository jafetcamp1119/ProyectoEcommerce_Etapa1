import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { IRespuesta } from '../model/IAuth';
import {
  ICompraProveedorConfirmada,
  ICompraProveedorDetalle,
  IFiltroComprasProveedor,
  IPaginaComprasProveedor,
  ISolicitudCompraProveedor
} from '../model/ICompraProveedor';
import { environment } from '../../environments/environment';

const baseUrl = `${environment.baseUrl}/CompraProveedor`;

@Injectable({ providedIn: 'root' })
export class CompraProveedorService {
  private readonly http = inject(HttpClient);

  proforma(datos: ISolicitudCompraProveedor): Observable<Blob> {
    return this.http.post(`${baseUrl}/Proforma`, datos, { responseType: 'blob' });
  }

  enviarProforma(datos: ISolicitudCompraProveedor): Observable<IRespuesta<boolean>> {
    return this.http.post<IRespuesta<boolean>>(`${baseUrl}/EnviarProforma`, datos);
  }

  confirmar(datos: ISolicitudCompraProveedor): Observable<IRespuesta<ICompraProveedorConfirmada>> {
    return this.http.post<IRespuesta<ICompraProveedorConfirmada>>(`${baseUrl}/Confirmar`, datos);
  }

  historial(filtro: IFiltroComprasProveedor): Observable<IRespuesta<IPaginaComprasProveedor>> {
    let params = new HttpParams();
    for (const [clave, valor] of Object.entries(filtro)) {
      if (valor !== undefined && valor !== null && valor !== '') {
        params = params.set(clave, String(valor));
      }
    }
    return this.http.get<IRespuesta<IPaginaComprasProveedor>>(`${baseUrl}/Historial`, { params });
  }

  detalle(id: number): Observable<IRespuesta<ICompraProveedorDetalle>> {
    return this.http.get<IRespuesta<ICompraProveedorDetalle>>(`${baseUrl}/Detalle/${id}`);
  }

  pdf(id: number): Observable<Blob> {
    return this.http.get(`${baseUrl}/Pdf/${id}`, { responseType: 'blob' });
  }
}
