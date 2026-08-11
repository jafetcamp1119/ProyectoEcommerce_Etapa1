import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { authInterceptor } from './auth.interceptor';

const tokenKey = 'proyectoEcommerceToken';
const usuarioKey = 'proyectoEcommerceUsuario';

describe('authInterceptor', () => {
  const navegar = jasmine.createSpy('navigate');

  beforeEach(() => {
    navegar.calls.reset();
    sessionStorage.setItem(tokenKey, crearToken(Date.now() + 60_000));
    sessionStorage.setItem(usuarioKey, JSON.stringify({
      usuarioId: 1,
      nombre: 'Cliente',
      apellidos: 'Prueba',
      correo: 'cliente@ejemplo.com',
      telefono: null,
      direccion: null,
      activo: true,
      fechaRegistro: '2026-08-09T00:00:00Z',
      rolId: 2,
      rolNombre: 'Cliente',
      menuOpciones: []
    }));

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: Router, useValue: { navigate: navegar } }
      ]
    });
  });

  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
    sessionStorage.removeItem(tokenKey);
    sessionStorage.removeItem(usuarioKey);
  });

  it('limpia la sesión y vuelve al login cuando la API responde 401', () => {
    TestBed.inject(HttpClient).get('/api/protegido').subscribe({ error: () => undefined });
    const solicitud = TestBed.inject(HttpTestingController).expectOne('/api/protegido');

    expect(solicitud.request.headers.get('Authorization')).toContain('Bearer ');
    solicitud.flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(sessionStorage.getItem(tokenKey)).toBeNull();
    expect(sessionStorage.getItem(usuarioKey)).toBeNull();
    expect(navegar).toHaveBeenCalledOnceWith(['/auth']);
  });
});

function crearToken(expiraEnMilisegundos: number): string {
  const payload = btoa(JSON.stringify({ exp: Math.floor(expiraEnMilisegundos / 1000) }))
    .replace(/=/g, '')
    .replace(/\+/g, '-')
    .replace(/\//g, '_');
  return `cabecera.${payload}.firma`;
}
