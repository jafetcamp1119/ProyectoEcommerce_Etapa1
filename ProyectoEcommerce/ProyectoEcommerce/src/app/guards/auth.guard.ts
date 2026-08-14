import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AutenticacionService } from '../services/autenticacion';

// un guard corre antes de abrir la ruta
// este deja pasar si hay JWT vigente o manda al login si no hay sesion
export const authGuard: CanActivateFn = () => {
  const autenticacion = inject(AutenticacionService);
  const router = inject(Router);

  // createUrlTree prepara la redireccion sin navegar a la fuerza desde el guard
  return autenticacion.estaAutenticado()
    ? true
    : router.createUrlTree(['/auth']);
};
