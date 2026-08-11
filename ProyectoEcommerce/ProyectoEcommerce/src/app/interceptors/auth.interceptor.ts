import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AutenticacionService } from '../services/autenticacion';

/**
 * Adjunta el JWT vigente como Bearer a las solicitudes y elimina la sesión
 * si la API responde 401 por token inválido, vencido o usuario ya no autorizado.
 */
export const authInterceptor: HttpInterceptorFn = (solicitud, siguiente) => {
  const autenticacion = inject(AutenticacionService);
  const router = inject(Router);
  const token = autenticacion.obtenerToken();
  const esInicioSesion = solicitud.url.endsWith('/Usuario/IniciarSesion');

  // El usuario y su identificador no se envían por separado: la API los obtiene de los claims firmados.
  const solicitudAutenticada = token
    ? solicitud.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : solicitud;

  return siguiente(solicitudAutenticada).pipe(
    catchError((error: HttpErrorResponse) => {
      // Un 401 del propio login representa credenciales incorrectas y no una sesión que deba redirigirse.
      if (error.status === 401 && !esInicioSesion) {
        autenticacion.cerrarSesion();
        void router.navigate(['/auth']);
      }

      return throwError(() => error);
    })
  );
};
