import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AutenticacionService } from '../services/autenticacion';

// un interceptor pasa por cada solicitud HTTP antes de mandarla
// aqui agrega el JWT como Bearer y limpia la sesion si la API responde 401
export const authInterceptor: HttpInterceptorFn = (solicitud, siguiente) => {
  const autenticacion = inject(AutenticacionService);
  const router = inject(Router);
  const token = autenticacion.obtenerToken();
  const esInicioSesion = solicitud.url.endsWith('/Usuario/IniciarSesion');

  // el usuario y su ID no viajan separados, la API los toma de los Claims firmados
  const solicitudAutenticada = token
    // clone crea una copia porque las solicitudes de Angular no se modifican directamente
    ? solicitud.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : solicitud;

  // siguiente manda la solicitud y catchError permite revisar una respuesta con error
  return siguiente(solicitudAutenticada).pipe(
    catchError((error: HttpErrorResponse) => {
      // un 401 del login significa credenciales incorrectas, no una sesion vencida que deba redirigirse
      if (error.status === 401 && !esInicioSesion) {
        autenticacion.cerrarSesion();
        void router.navigate(['/auth']);
      }

      // vuelve a lanzar el error para que tambien lo reciba el componente que hizo la llamada
      return throwError(() => error);
    })
  );
};
