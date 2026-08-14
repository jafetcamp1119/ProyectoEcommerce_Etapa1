import { ChangeDetectorRef, Component, inject, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AutenticacionService } from '../../services/autenticacion';

/** Muestra el tiempo restante cuando la API bloquea temporalmente los intentos de acceso. */
@Component({ selector: 'app-acceso-bloqueado', imports: [], templateUrl: './acceso-bloqueado.html', styleUrl: './acceso-bloqueado.css' })
export class AccesoBloqueado implements OnInit, OnDestroy {
  private readonly autenticacion = inject(AutenticacionService);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);
  segundos = 0;
  private temporizador?: ReturnType<typeof setInterval>;

  get tiempo(): string {
    const minutos = Math.floor(this.segundos / 60);
    const segundos = this.segundos % 60;
    return `${minutos}:${segundos.toString().padStart(2, '0')}`;
  }
  // lee la fecha guardada y actualiza cada segundo el tiempo que falta
  ngOnInit(): void {
    this.actualizar();
    this.temporizador = setInterval(() => this.actualizar(), 1000);
  }
  // limpia el intervalo al salir para que no siga trabajando escondido
  ngOnDestroy(): void { if (this.temporizador) clearInterval(this.temporizador); }
  // quita el bloqueo local cuando ya termino y vuelve al login
  volver(): void {
    if (this.segundos > 0) return;
    this.autenticacion.limpiarBloqueo();
    void this.router.navigate(['/auth']);
  }
  private actualizar(): void {
    const valor = this.autenticacion.obtenerBloqueadoHasta();
    this.segundos = valor ? Math.max(0, Math.ceil((new Date(valor).getTime() - Date.now()) / 1000)) : 0;
    this.cdr.markForCheck();
  }
}
