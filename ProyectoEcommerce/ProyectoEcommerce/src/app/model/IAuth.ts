/** Contrato genérico de las respuestas enviadas por la API. */
export interface IRespuesta<T> {
  success: boolean;
  data: T | null;
  error: string;
}

export interface ILoginUsuario {
  correo: string;
  contrasena: string;
}

export interface IRegistroUsuario {
  nombre: string;
  apellidos: string;
  correo: string;
  telefono: string;
  contrasena: string;
  confirmarContrasena: string;
}

/** Datos no sensibles del usuario que Angular conserva durante la sesión de la pestaña. */
export interface IUsuarioSesion {
  usuarioId: number;
  nombre: string;
  apellidos: string;
  correo: string;
  telefono: string;
  direccion: string | null;
  activo: boolean;
  fechaRegistro: string;
  rolId: number;
  rolNombre: string;
  menuOpciones: IMenuOpcion[];
}

export interface IMenuOpcion {
  menuOpcionId: number;
  nombre: string;
  ruta: string;
  icono: string | null;
  orden: number;
}

export interface IEstadoAutenticacion {
  usuario: IUsuarioSesion | null;
  bloqueado: boolean;
  bloqueadoHasta: string | null;
  segundosRestantes: number | null;
}

export interface IAutenticacionRespuesta {
  token: string;
  expira: string;
  usuario: IUsuarioSesion;
}
