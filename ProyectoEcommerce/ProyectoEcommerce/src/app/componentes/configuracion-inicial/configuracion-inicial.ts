import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators
} from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AutenticacionService } from '../../services/autenticacion';

/** Formulario de un solo uso para crear el primer Administrador de LessPrice. */
@Component({
  selector: 'app-configuracion-inicial',
  imports: [ReactiveFormsModule],
  templateUrl: './configuracion-inicial.html',
  styleUrls: ['../autenticacion/autenticacion.css', './configuracion-inicial.css']
})
export class ConfiguracionInicial implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly autenticacion = inject(AutenticacionService);
  private readonly router = inject(Router);
  private readonly changeDetector = inject(ChangeDetectorRef);

  procesando = true;
  mensajeError = '';
  correoAutorizado = '';

  readonly configuracionForm = this.formBuilder.nonNullable.group({
    nombre: ['', [this.textoObligatorio, Validators.maxLength(80)]],
    apellidos: ['', [this.textoObligatorio, Validators.maxLength(120)]],
    correo: ['', [this.textoObligatorio, Validators.email, Validators.maxLength(120),
      (control: AbstractControl) => this.correoPermitido(control)]],
    telefono: ['', [this.textoObligatorio, Validators.maxLength(30)]],
    contrasena: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100)]],
    confirmarContrasena: ['', Validators.required]
  }, { validators: this.contrasenasIguales });

  ngOnInit(): void {
    this.autenticacion.cerrarSesion();
    this.autenticacion.obtenerEstadoConfiguracionInicial()
      .pipe(finalize(() => this.finalizarSolicitud()))
      .subscribe({
        next: estado => {
          if (!estado.requiereConfiguracionInicial) {
            void this.router.navigate(['/auth']);
            return;
          }

          this.correoAutorizado = estado.correoAdministradorInicial.trim().toLowerCase();
          this.configuracionForm.controls.correo.setValue(this.correoAutorizado);
          this.configuracionForm.controls.correo.updateValueAndValidity();
        },
        error: () => {
          this.mensajeError = 'No fue posible comprobar el estado de configuración inicial.';
        }
      });
  }

  crearAdministrador(): void {
    this.limpiarFormulario();
    this.configuracionForm.markAllAsTouched();
    if (this.configuracionForm.invalid || this.procesando) return;

    this.procesando = true;
    this.mensajeError = '';
    this.autenticacion.crearAdministradorInicial(this.configuracionForm.getRawValue())
      .pipe(finalize(() => this.finalizarSolicitud()))
      .subscribe({
        next: () => {
          this.autenticacion.cerrarSesion();
          void this.router.navigate(['/auth']);
        },
        error: error => {
          if (error.status === 409) {
            void this.router.navigate(['/auth']);
            return;
          }
          this.mensajeError = error.error?.error || 'No fue posible crear el Administrador inicial.';
        }
      });
  }

  tieneError(campo: string, error?: string): boolean {
    const control = this.configuracionForm.get(campo);
    if (!control || !(control.touched || control.dirty)) return false;
    return error ? control.hasError(error) : control.invalid;
  }

  private limpiarFormulario(): void {
    this.configuracionForm.patchValue({
      nombre: this.configuracionForm.controls.nombre.value.trim(),
      apellidos: this.configuracionForm.controls.apellidos.value.trim(),
      correo: this.configuracionForm.controls.correo.value.trim().toLowerCase(),
      telefono: this.configuracionForm.controls.telefono.value.trim()
    });
  }

  private finalizarSolicitud(): void {
    this.procesando = false;
    this.changeDetector.markForCheck();
  }

  private textoObligatorio(control: AbstractControl): ValidationErrors | null {
    return typeof control.value === 'string' && control.value.trim().length > 0
      ? null
      : { required: true };
  }

  private correoPermitido(control: AbstractControl): ValidationErrors | null {
    const correo = typeof control.value === 'string' ? control.value.trim().toLowerCase() : '';
    return this.correoAutorizado && correo === this.correoAutorizado
      ? null
      : { correoNoAutorizado: true };
  }

  private contrasenasIguales(grupo: AbstractControl): ValidationErrors | null {
    return grupo.get('contrasena')?.value === grupo.get('confirmarContrasena')?.value
      ? null
      : { contrasenasDiferentes: true };
  }
}
