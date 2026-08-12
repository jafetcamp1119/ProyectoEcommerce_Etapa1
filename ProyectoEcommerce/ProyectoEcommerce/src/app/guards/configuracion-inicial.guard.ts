import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AutenticacionService } from '../services/autenticacion';

/** Fuerza el setup cuando la base todavía no contiene un Administrador activo. */
export const configuracionCompletaGuard: CanActivateFn = () => {
  const autenticacion = inject(AutenticacionService);
  const router = inject(Router);

  return autenticacion.obtenerEstadoConfiguracionInicial().pipe(
    map(estado => {
      if (!estado.requiereConfiguracionInicial) return true;
      autenticacion.cerrarSesion();
      return router.createUrlTree(['/configuracion-inicial']);
    }),
    // Una interrupción de la API no se interpreta como permiso para crear un Administrador.
    catchError(() => of(true))
  );
};

/** Impide reutilizar manualmente la pantalla una vez creado el primer Administrador. */
export const configuracionDisponibleGuard: CanActivateFn = () => {
  const autenticacion = inject(AutenticacionService);
  const router = inject(Router);

  return autenticacion.obtenerEstadoConfiguracionInicial().pipe(
    map(estado => estado.requiereConfiguracionInicial
      ? true
      : router.createUrlTree(['/auth'])),
    catchError(() => of(router.createUrlTree(['/auth'])))
  );
};
