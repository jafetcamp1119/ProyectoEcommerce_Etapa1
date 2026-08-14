import { CurrencyPipe } from '@angular/common';
import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import {
  ICheckoutPreparacion,
  ICompraCompletada,
  IConfirmarCompraSolicitud
} from '../../model/ICheckout';
import { CarritoService } from '../../services/carrito';
import { OrdenService } from '../../services/orden';


// esta pantalla recoge los datos de entrega y el metodo de pago
// despues manda toda la informacion a la API para confirmar definitivamente la compra
@Component({
  selector: 'app-checkout',
  imports: [ReactiveFormsModule, RouterLink, CurrencyPipe],
  templateUrl: './checkout.html',
  styleUrl: './checkout.css'
})
export class Checkout implements OnInit {

  // servicios y herramientas que se usan dentro del componente
  private readonly fb = inject(FormBuilder);
  private readonly ordenes = inject(OrdenService);
  private readonly carritoServicio = inject(CarritoService);
  private readonly cdr = inject(ChangeDetectorRef);


  // aqui se guarda la informacion que viene de la API para preparar el checkout
  preparacion: ICheckoutPreparacion | null = null;

  // cuando la compra termina correctamente aqui se guarda el resultado final
  completada: ICompraCompletada | null = null;

  // solamente puede tener 1 2 o 3 porque son los pasos que existen en el checkout
  paso: 1 | 2 | 3 = 1;

  cargando = true;
  procesando = false;
  error = '';


  // formulario del primer paso con los datos de entrega
  readonly entrega = this.fb.nonNullable.group({

    nombreCompleto: [''],

    correo: [
      '',
      [
        Validators.required,
        Validators.email,
        Validators.maxLength(120)
      ]
    ],

    telefono: [''],

    direccion: [
      '',
      [
        Validators.required,
        Validators.minLength(10),
        Validators.maxLength(500)
      ]
    ],

    // requiredTrue obliga a que el checkbox quede marcado
    confirmarCorreo: [false, Validators.requiredTrue]

  });


  // formulario del segundo paso
  // solamente guarda el metodo de pago escogido
  readonly pago = this.fb.nonNullable.group({
    metodoPago: ['', Validators.required]
  });


  // ngOnInit se ejecuta automaticamente cuando se abre esta pantalla
  // pide a la API el Cliente y el carrito con los que se va a preparar la compra
  ngOnInit(): void {

    this.ordenes
      .prepararCheckout()

      // finalize se ejecuta cuando termina la solicitud
      // tanto si respondio bien como si fallo
      .pipe(
        finalize(() => {
          this.cargando = false;
          this.cdr.markForCheck();
        })
      )

      .subscribe({

        // si la API responde correctamente guarda la preparacion
        next: respuesta => {

          this.preparacion = respuesta.data;

          // intenta sacar los datos del Cliente que vienen dentro de la respuesta
          const cliente = respuesta.data?.cliente;

          // si el Cliente existe carga sus datos dentro del formulario
          if (cliente) {

            this.entrega.reset({
              nombreCompleto: cliente.nombreCompleto,
              correo: cliente.correo,
              telefono: cliente.telefono,

              // si direccion viene null usa un texto vacio
              direccion: cliente.direccion ?? '',

              // siempre empieza sin confirmar el correo
              confirmarCorreo: false
            });

          }
        },

        // si falla intenta mostrar el mensaje de la API
        // si no viene ninguno usa este mensaje general
        error: err => {
          this.error =
            err?.error?.error ||
            'No fue posible preparar la compra.';
        }

      });
  }


  // valida el correo y la direccion antes de pasar al metodo de pago
  continuarEntrega(): void {

    // muestra los errores de todos los campos si todavia hay alguno invalido
    this.entrega.markAllAsTouched();

    // si el formulario tiene algun error no continua
    if (this.entrega.invalid) {
      return;
    }

    // acomoda los espacios de la direccion antes de continuar
    this.entrega.controls.direccion.setValue(
      this.normalizarEspacios(
        this.entrega.controls.direccion.value
      )
    );

    // vuelve a revisar la direccion despues de normalizarla
    if (this.entrega.controls.direccion.invalid) {
      return;
    }

    // si todo esta correcto pasa al metodo de pago
    this.paso = 2;
  }


  // guarda una de las 2 opciones de pago permitidas por la API
  seleccionarPago(
    metodo: 'TARJETA' | 'EFECTIVO'
  ): void {

    this.pago.controls.metodoPago.setValue(metodo);
  }


  // revisa que exista un metodo de pago antes de pasar al resumen
  continuarPago(): void {

    this.pago.markAllAsTouched();

    if (this.pago.invalid) {
      return;
    }

    this.paso = 3;
  }


  // vuelve al paso anterior sin borrar los datos que ya estaban escritos
  volver(): void {

    // si estaba en el paso 3 vuelve al 2
    // desde cualquier otro paso vuelve al 1
    this.paso = this.paso === 3
      ? 2
      : 1;

    this.error = '';
  }


  // arma la solicitud final y la manda a la API para confirmar la compra
  confirmarCompra(): void {

    // evita mandar otra compra si ya se esta procesando
    // tambien revisa otra vez que entrega y pago sean validos
    if (
      this.procesando ||
      this.entrega.invalid ||
      this.pago.invalid
    ) {
      return;
    }


    // getRawValue agarra todos los valores actuales del formulario de entrega
    const entrega = this.entrega.getRawValue();

    // metodoPago internamente es string
    // aqui se indica que para esta solicitud solamente puede ser TARJETA o EFECTIVO
    const metodo =
      this.pago.controls.metodoPago.value as
      'TARJETA' | 'EFECTIVO';


    // arma exactamente los datos que necesita la API para confirmar la compra
    const solicitud: IConfirmarCompraSolicitud = {

      // quita espacios del correo y lo manda en minusculas
      correoDestino: entrega.correo
        .trim()
        .toLowerCase(),

      // acomoda los espacios de la direccion antes de enviarla
      direccionEnvio: this.normalizarEspacios(
        entrega.direccion
      ),

      metodoPago: metodo,

      correoConfirmado: entrega.confirmarCorreo
    };


    this.error = '';
    this.procesando = true;


    this.ordenes
      .confirmarCompra(solicitud)

      // cuando la solicitud termina vuelve a quitar procesando
      // asi se pueden volver a habilitar los botones
      .pipe(
        finalize(() => {
          this.procesando = false;
          this.cdr.markForCheck();
        })
      )

      .subscribe({

        // si la compra se confirma guarda el resultado que devolvio la API
        next: respuesta => {

          this.completada = respuesta.data;

          // reinicia el indicador del carrito porque despues de la compra ya no debe mostrar esos productos
          this.carritoServicio.reiniciarIndicador();
        },

        // si falla muestra el mensaje de la API o uno general
        error: err => {
          this.error =
            err?.error?.error ||
            'No fue posible confirmar la compra.';
        }

      });
  }


  // cambia el codigo interno del metodo de pago por el texto que se muestra en pantalla
  etiquetaMetodo(): string {

    return this.pago.controls.metodoPago.value === 'TARJETA'
      ? 'Tarjeta'
      : 'Efectivo';
  }


  // limpia los espacios de un texto antes de mandarlo
  private normalizarEspacios(valor: string): string {

    // trim quita espacios del inicio y del final
    // replace cambia varios espacios seguidos por uno solo
    return valor
      .trim()
      .replace(/\s+/g, ' ');
  }

}
