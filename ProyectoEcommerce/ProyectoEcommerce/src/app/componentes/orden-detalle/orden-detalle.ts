import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { IOrdenDetalleConsulta } from '../../model/IOrden';
import { OrdenService } from '../../services/orden';


// esta pantalla muestra el detalle completo de una orden
// tambien permite descargar la factura cuando esta disponible
@Component({
  selector: 'app-orden-detalle',
  imports: [
    RouterLink,
    CurrencyPipe,
    DatePipe
  ],
  templateUrl: './orden-detalle.html',
  styleUrl: './orden-detalle.css'
})
export class OrdenDetalle implements OnInit {

  // herramientas y servicios que usa este componente
  private readonly route = inject(ActivatedRoute);
  private readonly servicio = inject(OrdenService);
  private readonly cdr = inject(ChangeDetectorRef);


  // aqui se guarda la orden que devuelve la API
  // empieza en null porque todavia no se ha cargado
  orden: IOrdenDetalleConsulta | null = null;


  // controla si la informacion de la orden todavia se esta cargando
  cargando = true;


  // controla si en este momento se esta descargando la factura
  // tambien sirve para evitar mandar la misma solicitud varias veces
  descargando = false;


  // guarda cualquier mensaje de error que se tenga que mostrar
  error = '';


  // ngOnInit se ejecuta automaticamente cuando se abre esta pantalla
  // lee el ordenId de la URL y pide el detalle completo a la API
  ngOnInit(): void {

    // paramMap busca el parametro ordenId que viene dentro de la ruta
    // Number lo convierte de texto a numero
    const ordenId =
      Number(
        this.route.snapshot.paramMap.get('ordenId')
      );


    // revisa que el id sea un numero entero y mayor que 0
    if (
      !Number.isInteger(ordenId) ||
      ordenId <= 0
    ) {

      this.cargando = false;

      this.error =
        'La orden solicitada no es válida.';

      return;
    }


    // pide a la API la informacion de la orden y sus productos
    this.servicio
      .detalle(ordenId)

      // finalize se ejecuta cuando termina la solicitud
      // tanto si sale bien como si ocurre un error
      .pipe(
        finalize(() => {

          this.cargando = false;

          // le avisa a Angular que vuelva a revisar la pantalla
          this.cdr.markForCheck();

        })
      )

      .subscribe({

        // si todo sale bien guarda la orden recibida
        next: respuesta => {
          this.orden = respuesta.data;
        },


        // si devuelve 404 puede significar que la orden no existe
        // o que la persona actual no tiene permiso para verla
        error: err => {

          this.error =
            err?.status === 404
              ? 'No se encontró la orden o no tienes permiso para consultarla.'
              : 'No fue posible cargar la orden.';

        }

      });
  }


  // descarga el PDF solamente si la orden indica que tiene factura disponible
  descargarFactura(): void {

    // ?. permite revisar facturaDisponible aunque orden pueda ser null
    // tampoco deja iniciar otra descarga mientras ya existe una en proceso
    if (
      !this.orden?.facturaDisponible ||
      this.descargando
    ) {
      return;
    }


    this.error = '';

    this.descargando = true;


    // pide a la API el archivo PDF de esta orden
    this.servicio
      .descargarFactura(this.orden.ordenId)

      // cuando termine vuelve a habilitar el boton de descarga
      .pipe(
        finalize(() => {

          this.descargando = false;

          this.cdr.markForCheck();

        })
      )

      .subscribe({

        // archivo es el Blob que devuelve la API con el PDF
        next: archivo => {

          // crea una URL temporal que apunta al archivo recibido
          const url =
            URL.createObjectURL(archivo);


          // crea temporalmente un enlace HTML para iniciar la descarga
          const enlace =
            document.createElement('a');


          // pone el archivo temporal como destino del enlace
          enlace.href = url;


          // arma el nombre del PDF usando el numero de la orden
          // ! indica que en este punto sabemos que orden no es null
          enlace.download =
            `Factura-${this.orden!.numeroOrden}.pdf`;


          // simula un click para que el navegador descargue el archivo
          enlace.click();


          // libera la URL temporal de memoria cuando ya no hace falta
          URL.revokeObjectURL(url);

        },


        // si algo falla durante la descarga muestra este mensaje
        error: () => {
          this.error =
            'No fue posible descargar la factura.';
        }

      });
  }


  // convierte el codigo interno del metodo de pago
  // al texto que se quiere mostrar en pantalla
  etiquetaMetodo(
    metodo: string | null
  ): string {

    // TARJETA se muestra como Tarjeta
    // EFECTIVO se muestra como Efectivo
    // cualquier otro valor se devuelve tal como vino
    // si viene null usa No registrado
    return metodo === 'TARJETA'
      ? 'Tarjeta'
      : metodo === 'EFECTIVO'
        ? 'Efectivo'
        : metodo ?? 'No registrado';
  }

}
