import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AutenticacionService } from '../services/autenticacion';

/**
 * Restringe las pantallas de mantenimiento al rol Administrador de la sesión actual.
 */
export const adminGuard: CanActivateFn = () => {
  const autenticacion = inject(AutenticacionService);
  const router = inject(Router);
  return autenticacion.estaAutenticado() && autenticacion.esAdministrador()
    ? true
    : router.createUrlTree(['/']);
};
