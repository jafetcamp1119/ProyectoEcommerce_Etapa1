import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AutenticacionService } from './autenticacion';
import { IUsuarioSesion } from '../model/IAuth';

const tokenKey = 'proyectoEcommerceToken';
const usuarioKey = 'proyectoEcommerceUsuario';

describe('AutenticacionService', () => {
  const usuario: IUsuarioSesion = {
    usuarioId: 1,
    nombre: 'Cliente',
    apellidos: 'Prueba',
    correo: 'cliente@ejemplo.com',
    telefono: '0000-0000',
    direccion: null,
    activo: true,
    fechaRegistro: '2026-08-09T00:00:00Z',
    rolId: 2,
    rolNombre: 'Cliente',
    menuOpciones: []
  };

  beforeEach(() => {
    sessionStorage.removeItem(tokenKey);
    sessionStorage.removeItem(usuarioKey);
    localStorage.removeItem(tokenKey);
    localStorage.removeItem(usuarioKey);
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
  });

  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
    sessionStorage.removeItem(tokenKey);
    sessionStorage.removeItem(usuarioKey);
    localStorage.removeItem(tokenKey);
    localStorage.removeItem(usuarioKey);
  });

  it('guarda la autenticación solamente en sessionStorage', () => {
    const servicio = TestBed.inject(AutenticacionService);
    const http = TestBed.inject(HttpTestingController);
    const token = crearToken(Date.now() + 60_000);

    servicio.iniciarSesion({ correo: usuario.correo, contrasena: 'secreto' }).subscribe();
    const solicitud = http.expectOne(x => x.url.endsWith('/Usuario/IniciarSesion'));
    solicitud.flush({
      success: true,
      data: { token, expira: '2026-08-09T01:00:00Z', usuario },
      error: ''
    });

    expect(sessionStorage.getItem(tokenKey)).toBe(token);
    expect(sessionStorage.getItem(usuarioKey)).toContain(usuario.correo);
    expect(localStorage.getItem(tokenKey)).toBeNull();
    expect(localStorage.getItem(usuarioKey)).toBeNull();
  });

  it('elimina la sesión cuando el JWT está expirado', () => {
    sessionStorage.setItem(tokenKey, crearToken(Date.now() - 60_000));
    sessionStorage.setItem(usuarioKey, JSON.stringify(usuario));

    const servicio = TestBed.inject(AutenticacionService);

    expect(servicio.estaAutenticado()).toBeFalse();
    expect(sessionStorage.getItem(tokenKey)).toBeNull();
    expect(sessionStorage.getItem(usuarioKey)).toBeNull();
  });

  it('restaura una sesión vigente después de recargar la pestaña', () => {
    const token = crearToken(Date.now() + 60_000);
    sessionStorage.setItem(tokenKey, token);
    sessionStorage.setItem(usuarioKey, JSON.stringify(usuario));

    const servicio = TestBed.inject(AutenticacionService);

    expect(servicio.obtenerToken()).toBe(token);
    expect(servicio.usuarioActual()?.correo).toBe(usuario.correo);
  });

  it('elimina la sesión cuando el JWT es inválido', () => {
    sessionStorage.setItem(tokenKey, 'token-invalido');
    sessionStorage.setItem(usuarioKey, JSON.stringify(usuario));

    const servicio = TestBed.inject(AutenticacionService);

    expect(servicio.estaAutenticado()).toBeFalse();
    expect(sessionStorage.getItem(tokenKey)).toBeNull();
    expect(sessionStorage.getItem(usuarioKey)).toBeNull();
  });

  it('limpia la autenticación antigua de localStorage y el logout elimina la sesión', () => {
    localStorage.setItem(tokenKey, crearToken(Date.now() + 60_000));
    localStorage.setItem(usuarioKey, JSON.stringify(usuario));
    const servicio = TestBed.inject(AutenticacionService);

    expect(localStorage.getItem(tokenKey)).toBeNull();
    expect(localStorage.getItem(usuarioKey)).toBeNull();

    sessionStorage.setItem(tokenKey, crearToken(Date.now() + 60_000));
    sessionStorage.setItem(usuarioKey, JSON.stringify(usuario));
    servicio.cerrarSesion();

    expect(sessionStorage.getItem(tokenKey)).toBeNull();
    expect(sessionStorage.getItem(usuarioKey)).toBeNull();
    expect(servicio.usuarioActual()).toBeNull();
  });
});

function crearToken(expiraEnMilisegundos: number): string {
  const payload = btoa(JSON.stringify({ exp: Math.floor(expiraEnMilisegundos / 1000) }))
    .replace(/=/g, '')
    .replace(/\+/g, '-')
    .replace(/\//g, '_');
  return `cabecera.${payload}.firma`;
}
