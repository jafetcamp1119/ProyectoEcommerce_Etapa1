import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import {
  IAutenticacionRespuesta,
  IEstadoConfiguracionInicial,
  ILoginUsuario,
  IRegistroUsuario,
  IRespuesta,
  IUsuarioSesion
} from '../model/IAuth';
import { environment } from '../../environments/environment';

const tokenKey = 'proyectoEcommerceToken';
const usuarioKey = 'proyectoEcommerceUsuario';
const bloqueoKey = 'proyectoEcommerceBloqueadoHasta';

// este servicio registra, inicia sesion y mantiene el usuario actual en memoria
// el JWT queda en sessionStorage para sobrevivir una recarga pero se borra al cerrar la pestaña
@Injectable({ providedIn: 'root' })
export class AutenticacionService {
  private readonly http = inject(HttpClient);
  private readonly usuarioSignal = signal<IUsuarioSesion | null>(null);

  // asReadonly deja que los componentes lean el usuario pero no lo cambien por fuera del servicio
  readonly usuarioActual = this.usuarioSignal.asReadonly();

  constructor() {
    // primero quita datos de versiones viejas y despues intenta restaurar la sesion de esta pestaña
    this.eliminarPersistenciaAnterior();
    this.restaurarSesion();
  }

  // manda el formulario de registro y devuelve un observable con la respuesta de la API
  registrar(datos: IRegistroUsuario): Observable<IRespuesta<IUsuarioSesion>> {
    return this.http.post<IRespuesta<IUsuarioSesion>>(
      `${environment.baseUrl}/Usuario/Registrar`,
      datos
    );
  }

  // pregunta si todavia falta crear el primer Administrador
  obtenerEstadoConfiguracionInicial(): Observable<IEstadoConfiguracionInicial> {
    return this.http.get<IEstadoConfiguracionInicial>(
      `${environment.baseUrl.replace(/\/api$/, '')}/api/auth/setup-status`
    );
  }

  // manda el formulario especial del setup inicial
  crearAdministradorInicial(datos: IRegistroUsuario): Observable<IRespuesta<IUsuarioSesion>> {
    return this.http.post<IRespuesta<IUsuarioSesion>>(
      `${environment.baseUrl.replace(/\/api$/, '')}/api/auth/setup-admin`,
      datos
    );
  }

  // manda correo y contraseña y guarda token y usuario solamente cuando la API responde bien
  iniciarSesion(datos: ILoginUsuario): Observable<IRespuesta<IAutenticacionRespuesta>> {
    return this.http.post<IRespuesta<IAutenticacionRespuesta>>(
      `${environment.baseUrl}/Usuario/IniciarSesion`,
      datos
    ).pipe(
      // tap hace este efecto sin cambiar la respuesta que sigue hacia el componente
      tap(respuesta => {
        if (respuesta.data) {
          sessionStorage.setItem(tokenKey, respuesta.data.token);
          // JSON.stringify convierte el objeto usuario en texto para poder guardarlo
          sessionStorage.setItem(usuarioKey, JSON.stringify(respuesta.data.usuario));
          this.usuarioSignal.set(respuesta.data.usuario);
          localStorage.removeItem(bloqueoKey);
        }
      })
    );
  }

  obtenerToken(): string | null {
    // nunca devuelve un token vencido, dañado o sin usuario asociado
    const token = sessionStorage.getItem(tokenKey);
    if (!token || !this.tokenVigente(token) || !this.usuarioSignal()) {
      this.cerrarSesion();
      return null;
    }

    return token;
  }

  estaAutenticado(): boolean {
    return this.obtenerToken() !== null;
  }

  // limpia datos actuales y tambien claves viejas que alguna version pudo dejar
  cerrarSesion(): void {
    sessionStorage.removeItem(tokenKey);
    sessionStorage.removeItem(usuarioKey);
    localStorage.removeItem(tokenKey);
    localStorage.removeItem(usuarioKey);
    this.usuarioSignal.set(null);
  }

  // ?. devuelve undefined si no hay usuario y la comparacion termina en false
  esAdministrador(): boolean {
    return this.usuarioSignal()?.rolNombre === 'Administrador';
  }

  esCliente(): boolean {
    return this.usuarioSignal()?.rolNombre === 'Cliente';
  }

  // cierra la sesion y conserva la fecha para mostrar la cuenta regresiva del bloqueo
  registrarBloqueo(bloqueadoHasta: string | null): void {
    this.cerrarSesion();
    if (bloqueadoHasta) localStorage.setItem(bloqueoKey, bloqueadoHasta);
  }

  obtenerBloqueadoHasta(): string | null { return localStorage.getItem(bloqueoKey); }
  limpiarBloqueo(): void { localStorage.removeItem(bloqueoKey); }

  // lee el texto guardado y lo convierte otra vez en objeto usuario
  private leerUsuario(): IUsuarioSesion | null {
    const valor = sessionStorage.getItem(usuarioKey);
    if (!valor) return null;

    try {
      return JSON.parse(valor) as IUsuarioSesion;
    } catch {
      sessionStorage.removeItem(usuarioKey);
      return null;
    }
  }

  // al recargar la pagina restaura solo si existen usuario y token y el JWT no vencio
  private restaurarSesion(): void {
    const token = sessionStorage.getItem(tokenKey);
    const usuario = this.leerUsuario();
    if (!token || !usuario || !this.tokenVigente(token)) {
      this.cerrarSesion();
      return;
    }

    this.usuarioSignal.set(usuario);
  }

  // elimina el formato viejo que guardaba la sesion por mas tiempo en localStorage
  private eliminarPersistenciaAnterior(): void {
    localStorage.removeItem(tokenKey);
    localStorage.removeItem(usuarioKey);
  }

  // abre solamente el payload del JWT para leer exp y comprobar la fecha
  // la API vuelve a validar firma, usuario y rol, esta revision solo evita mandar un token vencido
  private tokenVigente(token: string): boolean {
    try {
      // un JWT tiene encabezado.payload.firma y el indice 1 contiene el payload
      const segmento = token.split('.')[1];
      if (!segmento) return false;

      const base64 = segmento.replace(/-/g, '+').replace(/_/g, '/');
      // padEnd agrega los = que Base64 necesita cuando faltan
      const base64Completo = base64.padEnd(Math.ceil(base64.length / 4) * 4, '=');
      // atob decodifica Base64 y JSON.parse lo vuelve un objeto
      const payload = JSON.parse(atob(base64Completo)) as { exp?: number };
      return typeof payload.exp === 'number' && payload.exp * 1000 > Date.now();
    } catch {
      return false;
    }
  }
}
