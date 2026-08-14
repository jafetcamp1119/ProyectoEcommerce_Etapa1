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


// esta pantalla se usa una sola vez para crear el primer Administrador de LessPrice
@Component({
  selector: 'app-configuracion-inicial',
  imports: [ReactiveFormsModule],
  templateUrl: './configuracion-inicial.html',

  // usa los estilos generales de autenticacion y tambien los propios de esta pantalla
  styleUrls: [
    '../autenticacion/autenticacion.css',
    './configuracion-inicial.css'
  ]
})
export class ConfiguracionInicial implements OnInit {

  // herramientas y servicios que necesita este componente
  private readonly formBuilder = inject(FormBuilder);
  private readonly autenticacion = inject(AutenticacionService);
  private readonly router = inject(Router);
  private readonly changeDetector = inject(ChangeDetectorRef);


  // empieza en true porque al abrir la pantalla primero se consulta
  // si todavia hace falta hacer la configuracion inicial
  procesando = true;

  mensajeError = '';

  // formulario que pide los datos necesarios para crear la cuenta administradora inicial
  readonly configuracionForm = this.formBuilder.nonNullable.group({

    nombre: [
      '',
      [
        this.textoObligatorio,
        Validators.maxLength(80)
      ]
    ],

    apellidos: [
      '',
      [
        this.textoObligatorio,
        Validators.maxLength(120)
      ]
    ],

    correo: [
      '',
      [
        this.textoObligatorio,
        Validators.email,
        Validators.maxLength(120)
      ]
    ],

    telefono: [
      '',
      [
        this.textoObligatorio,
        Validators.maxLength(30)
      ]
    ],

    contrasena: [
      '',
      [
        Validators.required,
        Validators.minLength(8),
        Validators.maxLength(100)
      ]
    ],

    confirmarContrasena: [
      '',
      Validators.required
    ]

  }, {

    // esta validacion pertenece al formulario completo
    // porque necesita comparar 2 controles entre si
    validators: this.contrasenasIguales

  });


  // ngOnInit se ejecuta automaticamente cuando se abre esta pantalla
  // consulta si todavia hace falta crear el primer Administrador
  ngOnInit(): void {

    // cierra cualquier sesion que pudiera existir antes de entrar a la configuracion inicial
    this.autenticacion.cerrarSesion();

    this.autenticacion
      .obtenerEstadoConfiguracionInicial()

      // finalize siempre termina el estado procesando
      // tanto si la solicitud sale bien como si falla
      .pipe(
        finalize(() => this.finalizarSolicitud())
      )

      .subscribe({

        next: estado => {

          // si ya existe el Administrador inicial esta pantalla ya no se puede usar
          // por eso devuelve a la pagina de autenticacion
          if (!estado.requiereConfiguracionInicial) {
            void this.router.navigate(['/auth']);
            return;
          }

        },

        // si no se pudo revisar el estado muestra un mensaje general
        error: () => {
          this.mensajeError =
            'No fue posible comprobar el estado de configuración inicial.';
        }

      });
  }


  // valida los datos y manda la solicitud para crear el primer Administrador
  crearAdministrador(): void {

    // quita espacios innecesarios antes de validar y mandar los datos
    this.limpiarFormulario();

    // hace que se muestren los errores de los campos si hace falta
    this.configuracionForm.markAllAsTouched();

    // no continua si hay errores o si ya existe una solicitud en proceso
    if (
      this.configuracionForm.invalid ||
      this.procesando
    ) {
      return;
    }

    this.procesando = true;
    this.mensajeError = '';


    // getRawValue agarra todos los valores actuales del formulario
    this.autenticacion
      .crearAdministradorInicial(
        this.configuracionForm.getRawValue()
      )

      .pipe(
        finalize(() => this.finalizarSolicitud())
      )

      .subscribe({

        // si se pudo crear el Administrador cierra cualquier sesion
        // y manda a la pantalla normal de login
        next: () => {

          this.autenticacion.cerrarSesion();

          void this.router.navigate(['/auth']);

        },

        error: error => {

          // 409 significa que la configuracion inicial ya no esta disponible
          // por ejemplo porque el Administrador ya fue creado
          if (error.status === 409) {
            void this.router.navigate(['/auth']);
            return;
          }

          // intenta mostrar el mensaje que venga de la API
          // si no viene ninguno usa este mensaje general
          this.mensajeError =
            error.error?.error ||
            'No fue posible crear el Administrador inicial.';

        }

      });
  }


  // revisa si un campo ya debe mostrar un error en pantalla
  tieneError(
    campo: string,
    error?: string
  ): boolean {

    // busca el control dentro del formulario usando su nombre
    const control = this.configuracionForm.get(campo);

    // si no existe o la persona todavia no lo ha tocado ni cambiado no muestra nada
    if (
      !control ||
      !(control.touched || control.dirty)
    ) {
      return false;
    }

    // si llega un error especifico revisa solamente ese
    // si no llega ninguno revisa si el control tiene cualquier error
    return error
      ? control.hasError(error)
      : control.invalid;
  }


  // limpia los campos de texto antes de mandar los datos
  private limpiarFormulario(): void {

    // patchValue cambia solamente los campos indicados
    this.configuracionForm.patchValue({

      // trim quita espacios del inicio y del final
      nombre:
        this.configuracionForm.controls.nombre.value.trim(),

      apellidos:
        this.configuracionForm.controls.apellidos.value.trim(),

      // el correo tambien se convierte a minusculas
      correo:
        this.configuracionForm.controls.correo.value
          .trim()
          .toLowerCase(),

      telefono:
        this.configuracionForm.controls.telefono.value.trim()

    });
  }


  // se ejecuta cuando termina una solicitud
  private finalizarSolicitud(): void {

    this.procesando = false;

    // le avisa a Angular que vuelva a revisar la pantalla
    this.changeDetector.markForCheck();
  }


  // valida que un campo de texto no este vacio ni tenga solamente espacios
  private textoObligatorio(
    control: AbstractControl
  ): ValidationErrors | null {

    // si tiene texto correcto devuelve null porque no hay error
    // si esta vacio devuelve required true
    return typeof control.value === 'string' &&
      control.value.trim().length > 0
      ? null
      : { required: true };
  }


  // compara la contraseña con su confirmacion
  // este error se guarda en el formulario completo y no en un solo campo
  private contrasenasIguales(
    grupo: AbstractControl
  ): ValidationErrors | null {

    // si ambas tienen el mismo valor no hay error
    // si son diferentes agrega contrasenasDiferentes
    return grupo.get('contrasena')?.value ===
      grupo.get('confirmarContrasena')?.value
      ? null
      : { contrasenasDiferentes: true };
  }

}
