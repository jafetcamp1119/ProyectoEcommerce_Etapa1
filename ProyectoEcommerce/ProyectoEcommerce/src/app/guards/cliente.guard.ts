import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AutenticacionService } from '../services/autenticacion';

// protege carrito, checkout y catalogo para que solo entre una sesion de Cliente
export const clienteGuard: CanActivateFn = () => {
  const autenticacion = inject(AutenticacionService);
  const router = inject(Router);
  // una sesion con otro rol va a la pantalla que explica el bloqueo
  return autenticacion.estaAutenticado() && autenticacion.esCliente()
    ? true
    : router.createUrlTree(['/acceso-bloqueado']);
};
