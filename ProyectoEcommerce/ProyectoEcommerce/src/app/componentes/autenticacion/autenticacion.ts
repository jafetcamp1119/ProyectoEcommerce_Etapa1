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



// esta pantalla comparte login y registro y cambia entre los dos formularios
@Component({
  selector: 'app-autenticacion',
  imports: [ReactiveFormsModule],
  templateUrl: './autenticacion.html',
  styleUrl: './autenticacion.css'
})
export class Autenticacion {

  // inject sirve para poder usar estas herramientas o servicios dentro del componente
  // sin tener que crearlos manualmente
  private readonly formBuilder = inject(FormBuilder);
  private readonly autenticacion = inject(AutenticacionService);
  private readonly router = inject(Router);
  private readonly changeDetector = inject(ChangeDetectorRef);

  // estas variables controlan lo que se esta mostrando o pasando en la pantalla
  modoRegistro = false;
  procesando = false;
  mensajeError = '';
  mensajeExito = '';

  // nonNullable hace que los controles de texto siempre devuelvan string y no null
  // aqui tambien se ponen las reglas que debe cumplir cada campo del login
  readonly loginForm = this.formBuilder.nonNullable.group({
    correo: ['', [this.textoObligatorio, Validators.email, Validators.maxLength(120)]],
    contrasena: ['', Validators.required]
  });



  // este es el formulario de registro con todas sus validaciones
  // al final tambien usa contrasenasIguales para comparar las 2 contraseñas
  readonly registroForm = this.formBuilder.nonNullable.group({
    nombre: ['', [this.textoObligatorio, Validators.maxLength(80)]],
    apellidos: ['', [this.textoObligatorio, Validators.maxLength(120)]],
    correo: ['', [this.textoObligatorio, Validators.email, Validators.maxLength(120)]],
    telefono: ['', [this.textoObligatorio, Validators.maxLength(30)]],
    contrasena: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100)]],
    confirmarContrasena: ['', Validators.required]
  }, { validators: this.contrasenasIguales });



  // cambia entre registro y login y conserva el correo que la persona ya habia escrito
  cambiarModo(registro: boolean): void {

    // dependiendo de hacia donde vamos agarra el correo del formulario que estaba abierto
    const correo = registro
      ? this.loginForm.controls.correo.value
      : this.registroForm.controls.correo.value;

    this.modoRegistro = registro;
    this.mensajeError = '';
    this.mensajeExito = '';

    if (registro) {

      // reset limpia el formulario pero deja puesto el correo que ya tenia la persona
      this.registroForm.reset({
        correo: this.normalizarCorreo(correo)
      });

    } else {

      this.loginForm.reset({
        correo: this.normalizarCorreo(correo),
        contrasena: ''
      });

    }
  }



  // limpia el correo, muestra validaciones y manda el login si el formulario esta correcto
  iniciarSesion(): void {

    this.limpiarLogin();

    // hace que Angular marque todos los campos como tocados
    // asi si hay errores se muestran de una vez cuando la persona intenta entrar
    this.loginForm.markAllAsTouched();

    // si el formulario tiene errores o ya hay una solicitud en proceso no sigue
    if (this.loginForm.invalid || this.procesando) return;

    this.procesando = true;
    this.mensajeError = '';
    this.mensajeExito = '';



    // getRawValue agarra todos los valores que actualmente tiene el formulario
    // finalize siempre quita el estado procesando aunque la solicitud salga bien o falle
    this.autenticacion.iniciarSesion(this.loginForm.getRawValue())
      .pipe(finalize(() => this.finalizarSolicitud()))
      .subscribe({

        // next entra aqui cuando la API responde correctamente
        next: respuesta => {

          // si la respuesta trae datos significa que el login se pudo completar
          if (respuesta.data) {
            void this.router.navigate(['/']);
          }

        },


        // error entra aqui cuando la API devuelve algun problema
        error: error => {

          // 423 es el codigo que usa la API cuando la cuenta quedo bloqueada por intentos
          if (error.status === 423) {

            // guarda hasta cuando dura el bloqueo para poder mostrar la cuenta regresiva
            this.autenticacion.registrarBloqueo(
              error.error?.data?.bloqueadoHasta ?? null
            );

            this.loginForm.reset({
              correo: '',
              contrasena: ''
            });

            this.registroForm.reset();

            // manda a la pantalla que muestra el tiempo restante del bloqueo
            void this.router.navigate(['/acceso-bloqueado']);

            return;
          }

          // si fue 401 significa que las credenciales estaban malas
          // cualquier otro error muestra un mensaje mas general
          this.mensajeError = error.status === 401
            ? 'Correo o contraseña incorrectos.'
            : 'No fue posible iniciar sesión.';
        }

      });
  }



  // valida el registro, crea el Cliente y vuelve al login con el correo ya escrito
  crearCuenta(): void {

    this.limpiarRegistro();

    // muestra los errores de todos los campos si la persona intenta registrarse mal
    this.registroForm.markAllAsTouched();

    if (this.registroForm.invalid || this.procesando) return;

    this.procesando = true;
    this.mensajeError = '';
    this.mensajeExito = '';

    // guarda en datos todo lo que la persona escribio en el formulario
    const datos = this.registroForm.getRawValue();

    this.autenticacion.registrar(datos)
      .pipe(finalize(() => this.finalizarSolicitud()))
      .subscribe({

        // si la cuenta se crea correctamente vuelve al formulario de login
        next: () => {

          const correo = datos.correo;

          this.modoRegistro = false;

          // deja el correo listo para que solo tenga que poner la contraseña
          this.loginForm.reset({
            correo,
            contrasena: ''
          });

          this.registroForm.reset();

          this.mensajeExito = 'La cuenta fue creada correctamente';
        },

        // intenta mostrar el error que venga de la API
        // y si no viene ninguno usa este mensaje general
        error: error => {
          this.mensajeError =
            error.error?.error || 'No fue posible crear la cuenta.';
        }

      });
  }



  // busca un control y dice si ya debe mostrar su mensaje de validacion
  tieneError(
    formulario: 'login' | 'registro',
    campo: string,
    error?: string
  ): boolean {

    // dependiendo del formulario busca el campo en loginForm o registroForm
    const control = formulario === 'login'
      ? this.loginForm.get(campo)
      : this.registroForm.get(campo);

    // touched significa que la persona ya entro al campo
    // dirty significa que ya cambio su contenido
    if (!control || !(control.touched || control.dirty)) return false;

    // si llega un nombre de error revisa solo ese
    // si no llega ninguno revisa si el campo tiene cualquier error
    return error
      ? control.hasError(error)
      : control.invalid;
  }



  // deja el correo sin espacios y en minusculas antes de enviarlo
  private limpiarLogin(): void {

    this.loginForm.controls.correo.setValue(
      this.normalizarCorreo(
        this.loginForm.controls.correo.value
      )
    );

  }



  // patchValue cambia solamente los campos que se indican aqui
  // por eso puede limpiar estos textos sin tener que reiniciar todo el formulario
  private limpiarRegistro(): void {

    this.registroForm.patchValue({

      // trim quita espacios que hayan quedado al principio o al final
      nombre: this.registroForm.controls.nombre.value.trim(),
      apellidos: this.registroForm.controls.apellidos.value.trim(),

      correo: this.normalizarCorreo(
        this.registroForm.controls.correo.value
      ),

      telefono: this.registroForm.controls.telefono.value.trim()

    });

  }



  // sirve para que todos los correos queden guardados de la misma forma
  // trim quita espacios y toLowerCase lo convierte a minusculas
  private normalizarCorreo(correo: string): string {
    return correo.trim().toLowerCase();
  }



  // vuelve a habilitar botones y avisa a Angular que revise la vista
  private finalizarSolicitud(): void {

    this.procesando = false;

    // markForCheck le dice a Angular que vuelva a revisar la pantalla
    // para que se refleje el cambio de procesando
    this.changeDetector.markForCheck();

  }



  // considera invalido un texto que solo trae espacios
  private textoObligatorio(
    control: AbstractControl
  ): ValidationErrors | null {

    // si tiene texto valido devuelve null porque no hay error
    // si esta vacio devuelve required true
    return typeof control.value === 'string'
      && control.value.trim().length > 0
      ? null
      : { required: true };

  }



  // compara las dos contraseñas del grupo y agrega un error al formulario completo
  private contrasenasIguales(
    grupo: AbstractControl
  ): ValidationErrors | null {

    // el ? evita error si por alguna razon no encuentra alguno de los controles
    const contrasena =
      grupo.get('contrasena')?.value;

    const confirmacion =
      grupo.get('confirmarContrasena')?.value;

    // si son iguales no hay error
    // si son diferentes manda el error contrasenasDiferentes
    return contrasena === confirmacion
      ? null
      : { contrasenasDiferentes: true };

  }

}
