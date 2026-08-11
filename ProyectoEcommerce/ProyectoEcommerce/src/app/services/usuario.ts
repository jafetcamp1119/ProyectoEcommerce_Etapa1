import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { IRespuesta } from '../model/IAuth';
import { IPaginaUsuarios, IRol, IUsuario } from '../model/IUsuario';

/**
 * Consume la administración paginada de usuarios, roles y estados de cuenta.
 */
@Injectable({ providedIn: 'root' })
export class UsuarioService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.baseUrl}/Usuario`;

  listarAdministracion(filtro: { texto?: string; rolId?: number; activo?: boolean; pagina: number; tamanoPagina: number }): Observable<IRespuesta<IPaginaUsuarios>> {
    let params = new HttpParams().set('pagina', filtro.pagina).set('tamanoPagina', filtro.tamanoPagina);
    if (filtro.texto) params = params.set('texto', filtro.texto);
    if (filtro.rolId) params = params.set('rolId', filtro.rolId);
    if (filtro.activo !== undefined) params = params.set('activo', filtro.activo);
    return this.http.get<IRespuesta<IPaginaUsuarios>>(`${this.url}/ListarAdministracion`, { params });
  }
  listarRoles(): Observable<IRespuesta<IRol[]>> { return this.http.get<IRespuesta<IRol[]>>(`${this.url}/ListarRoles`); }
  cambiarRol(usuarioId: number, rolId: number): Observable<IRespuesta<IUsuario>> { return this.http.put<IRespuesta<IUsuario>>(`${this.url}/CambiarRol`, { usuarioId, rolId }); }
  cambiarEstado(usuarioId: number, activo: boolean): Observable<IRespuesta<IUsuario>> { return this.http.put<IRespuesta<IUsuario>>(`${this.url}/CambiarEstado`, { usuarioId, activo }); }
}
