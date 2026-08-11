import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { IImpuesto } from '../model/IImpuesto';
import { environment } from '../../environments/environment';
import { IRespuesta } from '../model/IAuth';

const baseUrl = environment.baseUrl;

/** Gestiona las llamadas administrativas relacionadas con impuestos y su vigencia. */
@Injectable({ providedIn: 'root' })
export class ImpuestoService {
  private http = inject(HttpClient);
  listar(): Observable<IRespuesta<IImpuesto[]>> { return this.http.get<IRespuesta<IImpuesto[]>>(`${baseUrl}/Impuesto/Listar`); }
  obtener(id: number): Observable<IRespuesta<IImpuesto>> { return this.http.get<IRespuesta<IImpuesto>>(`${baseUrl}/Impuesto/Obtener/${id}`); }
  buscar(nombre: string): Observable<IRespuesta<IImpuesto[]>> { return this.http.get<IRespuesta<IImpuesto[]>>(`${baseUrl}/Impuesto/Buscar`, { params: { nombre } }); }
  insertar(datos: IImpuesto): Observable<IRespuesta<IImpuesto>> { return this.http.post<IRespuesta<IImpuesto>>(`${baseUrl}/Impuesto/Insertar`, datos); }
  modificar(datos: IImpuesto): Observable<IRespuesta<IImpuesto>> { return this.http.put<IRespuesta<IImpuesto>>(`${baseUrl}/Impuesto/Modificar`, datos); }
  eliminar(id: number): Observable<IRespuesta<boolean>> { return this.http.delete<IRespuesta<boolean>>(`${baseUrl}/Impuesto/Eliminar/${id}`); }
}
