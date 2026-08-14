import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AutenticacionService } from '../services/autenticacion';

// deja entrar a mantenimientos solo cuando hay sesion y el rol es Administrador
export const adminGuard: CanActivateFn = () => {
  const autenticacion = inject(AutenticacionService);
  const router = inject(Router);
  // si falla cualquiera de las dos revisiones vuelve al inicio
  return autenticacion.estaAutenticado() && autenticacion.esAdministrador()
    ? true
    : router.createUrlTree(['/']);
};
