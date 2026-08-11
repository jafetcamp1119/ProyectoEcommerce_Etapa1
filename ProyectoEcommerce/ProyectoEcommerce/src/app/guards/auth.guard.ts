import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AutenticacionService } from '../services/autenticacion';

/**
 * Impide abrir el dashboard cuando no existe una sesión JWT vigente.
 */
export const authGuard: CanActivateFn = () => {
  const autenticacion = inject(AutenticacionService);
  const router = inject(Router);

  return autenticacion.estaAutenticado()
    ? true
    : router.createUrlTree(['/auth']);
};
