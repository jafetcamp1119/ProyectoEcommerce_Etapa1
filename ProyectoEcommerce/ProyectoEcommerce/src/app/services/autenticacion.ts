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

/**
 * Registra usuarios, inicia sesión y mantiene en memoria el usuario autenticado.
 * El JWT se conserva en sessionStorage para sobrevivir una recarga de la pestaña
 * sin persistir indefinidamente entre nuevas sesiones del navegador.
 */
@Injectable({ providedIn: 'root' })
export class AutenticacionService {
  private readonly http = inject(HttpClient);
  private readonly usuarioSignal = signal<IUsuarioSesion | null>(null);

  readonly usuarioActual = this.usuarioSignal.asReadonly();

  constructor() {
    // La migración descarta tokens antiguos de localStorage antes de restaurar una sesión vigente.
    this.eliminarPersistenciaAnterior();
    this.restaurarSesion();
  }

  registrar(datos: IRegistroUsuario): Observable<IRespuesta<IUsuarioSesion>> {
    return this.http.post<IRespuesta<IUsuarioSesion>>(
      `${environment.baseUrl}/Usuario/Registrar`,
      datos
    );
  }

  obtenerEstadoConfiguracionInicial(): Observable<IEstadoConfiguracionInicial> {
    return this.http.get<IEstadoConfiguracionInicial>(
      `${environment.baseUrl.replace(/\/api$/, '')}/api/auth/setup-status`
    );
  }

  crearAdministradorInicial(datos: IRegistroUsuario): Observable<IRespuesta<IUsuarioSesion>> {
    return this.http.post<IRespuesta<IUsuarioSesion>>(
      `${environment.baseUrl.replace(/\/api$/, '')}/api/auth/setup-admin`,
      datos
    );
  }

  iniciarSesion(datos: ILoginUsuario): Observable<IRespuesta<IAutenticacionRespuesta>> {
    return this.http.post<IRespuesta<IAutenticacionRespuesta>>(
      `${environment.baseUrl}/Usuario/IniciarSesion`,
      datos
    ).pipe(
      tap(respuesta => {
        if (respuesta.data) {
          sessionStorage.setItem(tokenKey, respuesta.data.token);
          sessionStorage.setItem(usuarioKey, JSON.stringify(respuesta.data.usuario));
          this.usuarioSignal.set(respuesta.data.usuario);
          localStorage.removeItem(bloqueoKey);
        }
      })
    );
  }

  obtenerToken(): string | null {
    // Nunca se envía un token vencido, dañado o sin datos de usuario asociados.
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

  cerrarSesion(): void {
    // También se limpian las claves antiguas para evitar que una versión previa restaure la sesión.
    sessionStorage.removeItem(tokenKey);
    sessionStorage.removeItem(usuarioKey);
    localStorage.removeItem(tokenKey);
    localStorage.removeItem(usuarioKey);
    this.usuarioSignal.set(null);
  }

  esAdministrador(): boolean {
    return this.usuarioSignal()?.rolNombre === 'Administrador';
  }

  esCliente(): boolean {
    return this.usuarioSignal()?.rolNombre === 'Cliente';
  }

  registrarBloqueo(bloqueadoHasta: string | null): void {
    this.cerrarSesion();
    if (bloqueadoHasta) localStorage.setItem(bloqueoKey, bloqueadoHasta);
  }

  obtenerBloqueadoHasta(): string | null { return localStorage.getItem(bloqueoKey); }
  limpiarBloqueo(): void { localStorage.removeItem(bloqueoKey); }

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

  private restaurarSesion(): void {
    const token = sessionStorage.getItem(tokenKey);
    const usuario = this.leerUsuario();
    if (!token || !usuario || !this.tokenVigente(token)) {
      this.cerrarSesion();
      return;
    }

    this.usuarioSignal.set(usuario);
  }

  private eliminarPersistenciaAnterior(): void {
    localStorage.removeItem(tokenKey);
    localStorage.removeItem(usuarioKey);
  }

  private tokenVigente(token: string): boolean {
    try {
      const segmento = token.split('.')[1];
      if (!segmento) return false;

      const base64 = segmento.replace(/-/g, '+').replace(/_/g, '/');
      const base64Completo = base64.padEnd(Math.ceil(base64.length / 4) * 4, '=');
      const payload = JSON.parse(atob(base64Completo)) as { exp?: number };
      return typeof payload.exp === 'number' && payload.exp * 1000 > Date.now();
    } catch {
      return false;
    }
  }
}
