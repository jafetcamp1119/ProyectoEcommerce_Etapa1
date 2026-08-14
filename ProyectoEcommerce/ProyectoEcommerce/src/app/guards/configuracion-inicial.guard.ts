import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AutenticacionService } from '../services/autenticacion';

// antes de abrir la app pregunta a la API si todavia falta el primer Administrador
export const configuracionCompletaGuard: CanActivateFn = () => {
  const autenticacion = inject(AutenticacionService);
  const router = inject(Router);

  // pipe deja transformar la respuesta del observable antes de que el router la use
  return autenticacion.obtenerEstadoConfiguracionInicial().pipe(
    map(estado => {
      if (!estado.requiereConfiguracionInicial) return true;
      autenticacion.cerrarSesion();
      return router.createUrlTree(['/configuracion-inicial']);
    }),
    // catchError evita usar una falla de red como permiso para crear otro Administrador
    catchError(() => of(true))
  );
};

// protege la ruta del setup para que no se pueda volver a abrir cuando ya hay Administrador
export const configuracionDisponibleGuard: CanActivateFn = () => {
  const autenticacion = inject(AutenticacionService);
  const router = inject(Router);

  return autenticacion.obtenerEstadoConfiguracionInicial().pipe(
    // map devuelve true para entrar o una UrlTree para volver al login
    map(estado => estado.requiereConfiguracionInicial
      ? true
      : router.createUrlTree(['/auth'])),
    catchError(() => of(router.createUrlTree(['/auth'])))
  );
};
