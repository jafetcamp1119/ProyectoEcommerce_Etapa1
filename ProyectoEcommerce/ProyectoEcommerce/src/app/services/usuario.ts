import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { IRespuesta } from '../model/IAuth';
import { IPaginaUsuarios, IRol, IUsuario } from '../model/IUsuario';

@Injectable({
  providedIn: 'root'
})
export class UsuarioService {

  private readonly http = inject(HttpClient);
  private readonly url = `${environment.baseUrl}/Usuario`;


  // ========================= USUARIOS =========================

  // arma los parametros de paginacion y agrega solo los filtros que tienen valor
  listarAdministracion(
    filtro: {
      texto?: string;
      rolId?: number;
      activo?: boolean;
      pagina: number;
      tamanoPagina: number;
    }
  ): Observable<IRespuesta<IPaginaUsuarios>> {

    let params = new HttpParams()
      .set('pagina', filtro.pagina)
      .set('tamanoPagina', filtro.tamanoPagina);

    if (filtro.texto) {
      params = params.set('texto', filtro.texto);
    }

    if (filtro.rolId) {
      params = params.set('rolId', filtro.rolId);
    }

    // se revisa contra undefined porque false tambien es un valor valido
    if (filtro.activo !== undefined) {
      params = params.set('activo', filtro.activo);
    }

    return this.http.get<IRespuesta<IPaginaUsuarios>>(
      `${this.url}/ListarAdministracion`,
      { params }
    );
  }


  // ========================= ROLES =========================

  listarRoles(): Observable<IRespuesta<IRol[]>> {
    return this.http.get<IRespuesta<IRol[]>>(
      `${this.url}/ListarRoles`
    );
  }

  cambiarRol(
    usuarioId: number,
    rolId: number
  ): Observable<IRespuesta<IUsuario>> {

    return this.http.put<IRespuesta<IUsuario>>(
      `${this.url}/CambiarRol`,
      { usuarioId, rolId }
    );
  }


  // ========================= ESTADO =========================

  cambiarEstado(
    usuarioId: number,
    activo: boolean
  ): Observable<IRespuesta<IUsuario>> {

    return this.http.put<IRespuesta<IUsuario>>(
      `${this.url}/CambiarEstado`,
      { usuarioId, activo }
    );
  }

}
