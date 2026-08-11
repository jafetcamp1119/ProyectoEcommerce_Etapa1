import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { IRol, IUsuario } from '../../model/IUsuario';
import { UsuarioService } from '../../services/usuario';

/** Pantalla administrativa para filtrar usuarios y cambiar sus roles o estados. */
@Component({ selector: 'app-usuario', imports: [ReactiveFormsModule], templateUrl: './usuario.html', styleUrl: './usuario.css' })
export class Usuario implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly servicio = inject(UsuarioService);
  private readonly cdr = inject(ChangeDetectorRef);
  usuarios: IUsuario[] = [];
  roles: IRol[] = [];
  rolesSeleccionados: Record<number, number> = {};
  cargando = true;
  guardandoId = 0;
  mensaje = '';
  error = '';
  pagina = 1;
  tamanoPagina = 25;
  total = 0;
  readonly tamanos = [25, 50, 75, 100];
  readonly filtros = this.fb.nonNullable.group({ texto: '', rolId: '', estado: '' });
  get totalPaginas(): number { return Math.max(1, Math.ceil(this.total / this.tamanoPagina)); }

  ngOnInit(): void {
    this.servicio.listarRoles().subscribe({
      next: r => { this.roles = r.data ?? []; this.cdr.markForCheck(); },
      error: e => { this.error = this.mensajeError(e); this.cdr.markForCheck(); }
    });
    this.cargar();
  }
  cargar(): void {
    const f = this.filtros.getRawValue();
    this.cargando = true; this.error = '';
    this.servicio.listarAdministracion({
      texto: f.texto.trim() || undefined,
      rolId: f.rolId ? Number(f.rolId) : undefined,
      activo: f.estado === '' ? undefined : f.estado === 'activo',
      pagina: this.pagina,
      tamanoPagina: this.tamanoPagina
    }).pipe(finalize(() => { this.cargando = false; this.cdr.markForCheck(); })).subscribe({
      next: r => {
        this.usuarios = r.data?.items ?? [];
        this.total = r.data?.total ?? 0;
        this.rolesSeleccionados = Object.fromEntries(this.usuarios.map(x => [x.usuarioId, x.rolId]));
      },
      error: e => this.error = this.mensajeError(e)
    });
  }
  buscar(): void { this.pagina = 1; this.cargar(); }
  limpiar(): void { this.filtros.reset({ texto: '', rolId: '', estado: '' }); this.pagina = 1; this.cargar(); }
  seleccionarRol(usuarioId: number, valor: string): void { this.rolesSeleccionados[usuarioId] = Number(valor); }
  guardarRol(usuario: IUsuario): void {
    const rolId = this.rolesSeleccionados[usuario.usuarioId];
    if (!rolId || rolId === usuario.rolId || this.guardandoId) return;
    this.guardandoId = usuario.usuarioId; this.limpiarMensajes();
    this.servicio.cambiarRol(usuario.usuarioId, rolId).pipe(finalize(() => { this.guardandoId = 0; this.cdr.markForCheck(); })).subscribe({
      next: () => { this.mensaje = 'Rol actualizado correctamente.'; this.cargar(); },
      error: e => { this.rolesSeleccionados[usuario.usuarioId] = usuario.rolId; this.error = this.mensajeError(e); }
    });
  }
  cambiarEstado(usuario: IUsuario): void {
    if (this.guardandoId) return;
    this.guardandoId = usuario.usuarioId; this.limpiarMensajes();
    this.servicio.cambiarEstado(usuario.usuarioId, !usuario.activo).pipe(finalize(() => { this.guardandoId = 0; this.cdr.markForCheck(); })).subscribe({
      next: () => { this.mensaje = usuario.activo ? 'Usuario desactivado.' : 'Usuario activado.'; this.cargar(); },
      error: e => this.error = this.mensajeError(e)
    });
  }
  cambiarTamano(valor: string): void { this.tamanoPagina = Number(valor); this.pagina = 1; this.cargar(); }
  irPagina(delta: number): void { this.pagina = Math.min(this.totalPaginas, Math.max(1, this.pagina + delta)); this.cargar(); }
  private limpiarMensajes(): void { this.mensaje = ''; this.error = ''; }
  private mensajeError(error: any): string { return error?.error?.error || error?.error?.title || 'No fue posible completar la operación.'; }
}
