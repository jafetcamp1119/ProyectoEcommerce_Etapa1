// usuario que se muestra en administracion, nunca incluye contraseña ni PasswordHash
export interface IUsuario {
  usuarioId: number;
  rolId: number;
  rolNombre: string;
  nombre: string;
  apellidos: string;
  correo: string;
  telefono: string;
  direccion: string | null;
  activo: boolean;
  fechaRegistro: string;
}

export interface IRol { rolId: number; nombre: string; }
export interface IPaginaUsuarios { items: IUsuario[]; pagina: number; tamanoPagina: number; total: number; }
