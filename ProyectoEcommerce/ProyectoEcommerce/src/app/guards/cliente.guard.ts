import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AutenticacionService } from '../services/autenticacion';

/**
 * Protege carrito, checkout y navegación comercial para usuarios con rol Cliente.
 */
export const clienteGuard: CanActivateFn = () => {
  const autenticacion = inject(AutenticacionService);
  const router = inject(Router);
  return autenticacion.estaAutenticado() && autenticacion.esCliente()
    ? true
    : router.createUrlTree(['/acceso-bloqueado']);
};
