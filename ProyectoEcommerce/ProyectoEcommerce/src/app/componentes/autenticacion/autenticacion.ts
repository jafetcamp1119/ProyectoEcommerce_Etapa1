import { ChangeDetectorRef, Component, inject } from '@angular/core';
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

/**
 * Presenta inicio de sesión y registro y comunica sus resultados al servicio de autenticación.
 */
@Component({
  selector: 'app-autenticacion',
  imports: [ReactiveFormsModule],
  templateUrl: './autenticacion.html',
  styleUrl: './autenticacion.css'
})
export class Autenticacion {
  private readonly formBuilder = inject(FormBuilder);
  private readonly autenticacion = inject(AutenticacionService);
  private readonly router = inject(Router);
  private readonly changeDetector = inject(ChangeDetectorRef);

  modoRegistro = false;
  procesando = false;
  mensajeError = '';
  mensajeExito = '';

  readonly loginForm = this.formBuilder.nonNullable.group({
    correo: ['', [this.textoObligatorio, Validators.email, Validators.maxLength(120)]],
    contrasena: ['', Validators.required]
  });

  readonly registroForm = this.formBuilder.nonNullable.group({
    nombre: ['', [this.textoObligatorio, Validators.maxLength(80)]],
    apellidos: ['', [this.textoObligatorio, Validators.maxLength(120)]],
    correo: ['', [this.textoObligatorio, Validators.email, Validators.maxLength(120)]],
    telefono: ['', [this.textoObligatorio, Validators.maxLength(30)]],
    contrasena: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100)]],
    confirmarContrasena: ['', Validators.required]
  }, { validators: this.contrasenasIguales });

  cambiarModo(registro: boolean): void {
    const correo = registro
      ? this.loginForm.controls.correo.value
      : this.registroForm.controls.correo.value;

    this.modoRegistro = registro;
    this.mensajeError = '';
    this.mensajeExito = '';

    if (registro) {
      this.registroForm.reset({ correo: this.normalizarCorreo(correo) });
    } else {
      this.loginForm.reset({ correo: this.normalizarCorreo(correo), contrasena: '' });
    }
  }

  iniciarSesion(): void {
    this.limpiarLogin();
    this.loginForm.markAllAsTouched();
    if (this.loginForm.invalid || this.procesando) return;

    this.procesando = true;
    this.mensajeError = '';
    this.mensajeExito = '';

    this.autenticacion.iniciarSesion(this.loginForm.getRawValue())
      .pipe(finalize(() => this.finalizarSolicitud()))
      .subscribe({
        next: respuesta => {
          if (respuesta.data) {
            void this.router.navigate(['/']);
          }
        },
        error: error => {
          if (error.status === 423) {
            this.autenticacion.registrarBloqueo(error.error?.data?.bloqueadoHasta ?? null);
            this.loginForm.reset({ correo: '', contrasena: '' });
            this.registroForm.reset();
            void this.router.navigate(['/acceso-bloqueado']);
            return;
          }
          this.mensajeError = error.status === 401
            ? 'Correo o contraseña incorrectos.'
            : 'No fue posible iniciar sesión.';
        }
      });
  }

  crearCuenta(): void {
    this.limpiarRegistro();
    this.registroForm.markAllAsTouched();
    if (this.registroForm.invalid || this.procesando) return;

    this.procesando = true;
    this.mensajeError = '';
    this.mensajeExito = '';
    const datos = this.registroForm.getRawValue();

    this.autenticacion.registrar(datos)
      .pipe(finalize(() => this.finalizarSolicitud()))
      .subscribe({
        next: () => {
          const correo = datos.correo;
          this.modoRegistro = false;
          this.loginForm.reset({ correo, contrasena: '' });
          this.registroForm.reset();
          this.mensajeExito = 'La cuenta fue creada correctamente';
        },
        error: error => {
          this.mensajeError = error.error?.error || 'No fue posible crear la cuenta.';
        }
      });
  }

  tieneError(formulario: 'login' | 'registro', campo: string, error?: string): boolean {
    const control = formulario === 'login'
      ? this.loginForm.get(campo)
      : this.registroForm.get(campo);

    if (!control || !(control.touched || control.dirty)) return false;
    return error ? control.hasError(error) : control.invalid;
  }

  private limpiarLogin(): void {
    this.loginForm.controls.correo.setValue(
      this.normalizarCorreo(this.loginForm.controls.correo.value)
    );
  }

  private limpiarRegistro(): void {
    this.registroForm.patchValue({
      nombre: this.registroForm.controls.nombre.value.trim(),
      apellidos: this.registroForm.controls.apellidos.value.trim(),
      correo: this.normalizarCorreo(this.registroForm.controls.correo.value),
      telefono: this.registroForm.controls.telefono.value.trim()
    });
  }

  private normalizarCorreo(correo: string): string {
    return correo.trim().toLowerCase();
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

  private contrasenasIguales(grupo: AbstractControl): ValidationErrors | null {
    const contrasena = grupo.get('contrasena')?.value;
    const confirmacion = grupo.get('confirmarContrasena')?.value;
    return contrasena === confirmacion ? null : { contrasenasDiferentes: true };
  }
}
