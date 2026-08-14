import { CurrencyPipe } from '@angular/common';
import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ICarritoActual, ICarritoItem } from '../../model/ICarrito';
import { CarritoService } from '../../services/carrito';


// esta pantalla muestra todo lo que la persona tiene en el carrito
// tambien permite cambiar cantidades eliminar productos y decidir si puede seguir al checkout
@Component({
  selector: 'app-carrito',
  imports: [CurrencyPipe, RouterLink],
  templateUrl: './carrito.html',
  styleUrl: './carrito.css'
})
export class Carrito implements OnInit {

  // inject permite usar el servicio del carrito y otras herramientas de Angular dentro de este componente
  private readonly servicio = inject(CarritoService);
  private readonly cdr = inject(ChangeDetectorRef);

  // aqui se guarda el carrito completo que viene de la API
  // empieza en null porque cuando abre la pagina todavia no se ha cargado
  carrito: ICarritoActual | null = null;

  // controla si todavia estamos esperando que cargue el carrito
  cargando = true;

  // mensajes que se muestran en pantalla
  error = '';
  mensaje = '';

  // Set guarda los ids de los productos que en este momento se estan procesando
  // sirve para evitar que la persona toque varias veces los botones mientras la API responde
  procesando = new Set<number>();



  // este get revisa si el carrito esta listo para poder continuar al checkout
  // devuelve true o false dependiendo del estado de todos los productos
  get puedeContinuar(): boolean {

    // !! convierte el resultado en true o false
    // every revisa que TODOS los productos cumplan las condiciones que estan despues
    return !!this.carrito?.items.length &&
      this.carrito.items.every(
        x =>
          x.productoActivo &&
          x.stockSuficiente &&
          x.cantidad >= 1
      );
  }



  // ngOnInit se ejecuta automaticamente cuando se abre este componente
  // aqui pide a la API el carrito completo con los precios descuentos y stock actuales
  ngOnInit(): void {

    this.servicio.obtenerActual()

      // finalize se ejecuta siempre cuando termina la solicitud
      // no importa si la API respondio bien o si dio error
      .pipe(
        finalize(() => {
          this.cargando = false;

          // le avisa a Angular que revise la pantalla porque ya termino de cargar
          this.cdr.markForCheck();
        })
      )

      .subscribe({

        // next entra aqui cuando la API responde correctamente
        // guarda en carrito los datos que devolvio el backend
        next: respuesta => {
          this.carrito = respuesta.data;
        },

        // si falla intenta mostrar el mensaje que mando la API
        // y si no viene ninguno usa este mensaje general
        error: err => {
          this.error =
            err?.error?.error ||
            'No fue posible cargar el carrito.';
        }

      });
  }



  // este metodo se usa cuando se presiona + o -
  // recibe el producto y la nueva cantidad que queremos poner
  cambiarCantidad(item: ICarritoItem, cantidad: number): void {

    // Number.isInteger revisa que la cantidad sea un numero entero
    // tambien se revisa que no sea menor a 1 ni mayor al stock que queda
    if (
      !Number.isInteger(cantidad) ||
      cantidad < 1 ||
      cantidad > item.stockDisponible
    ) {

      // si se paso del stock muestra un mensaje
      // si el problema fue otro muestra el mensaje de numero entero
      this.error = cantidad > item.stockDisponible
        ? 'La cantidad no puede superar el stock disponible.'
        : 'La cantidad debe ser un número entero mayor o igual a 1.';

      return;
    }

    // si la cantidad nueva es igual a la que ya tenia no hace ninguna solicitud
    // tampoco sigue si este producto ya se esta procesando
    if (
      cantidad === item.cantidad ||
      this.procesando.has(item.carritoDetalleId)
    ) {
      return;
    }

    this.limpiarMensajes();

    // add mete el id dentro del Set para marcar este producto como ocupado
    this.procesando.add(item.carritoDetalleId);

    // manda a la API el id de la linea del carrito y la cantidad nueva
    this.servicio
      .actualizarCantidad(item.carritoDetalleId, cantidad)

      .pipe(
        finalize(() => {

          // delete saca el id del Set cuando ya termino la solicitud
          // con esto los botones del producto se pueden volver a usar
          this.procesando.delete(item.carritoDetalleId);

          this.cdr.markForCheck();
        })
      )

      .subscribe({

        // la API devuelve nuevamente el carrito ya calculado
        // asi no tenemos que calcular precios descuentos o totales aqui en Angular
        next: respuesta => {
          this.carrito = respuesta.data;
          this.mensaje = 'Cantidad actualizada.';
        },

        error: err => {
          this.error =
            err?.error?.error ||
            'No fue posible actualizar la cantidad.';
        }

      });
  }



  // este metodo se usa cuando la persona escribe directamente una cantidad en el input
  escribirCantidad(item: ICarritoItem, input: HTMLInputElement): void {

    // trim quita espacios que hayan quedado antes o despues del numero
    const texto = input.value.trim();

    // esta expresion regular revisa que el texto tenga solamente numeros
    // \d significa numero y + significa que tiene que haber uno o mas
    if (!/^\d+$/.test(texto)) {

      // si escribio algo invalido vuelve a poner la cantidad que tenia antes
      input.value = String(item.cantidad);

      this.error = 'Ingresa una cantidad entera válida.';
      return;
    }

    // Number convierte el texto del input a un numero de verdad
    const cantidad = Number(texto);

    // si la cantidad no esta dentro del rango permitido
    // deja visualmente el input con la cantidad anterior
    if (
      cantidad < 1 ||
      cantidad > item.stockDisponible
    ) {
      input.value = String(item.cantidad);
    }

    // aqui se reutiliza el mismo metodo que usa los botones + y -
    // ese metodo vuelve a validar antes de mandar algo a la API
    this.cambiarCantidad(item, cantidad);
  }



  // elimina completamente un producto del carrito
  // despues reemplaza el carrito con la version nueva que devuelve la API
  eliminar(item: ICarritoItem): void {

    // si este producto ya tiene una solicitud en proceso no hace otra
    if (this.procesando.has(item.carritoDetalleId)) {
      return;
    }

    this.limpiarMensajes();

    // marca esta linea como procesando mientras se elimina
    this.procesando.add(item.carritoDetalleId);

    this.servicio
      .eliminar(item.carritoDetalleId)

      .pipe(
        finalize(() => {

          // al terminar vuelve a habilitar las acciones de este producto
          this.procesando.delete(item.carritoDetalleId);
          this.cdr.markForCheck();
        })
      )

      .subscribe({

        // guarda el carrito actualizado sin el producto que se elimino
        next: respuesta => {
          this.carrito = respuesta.data;
          this.mensaje = 'Producto eliminado del carrito.';
        },

        error: err => {
          this.error =
            err?.error?.error ||
            'No fue posible eliminar el producto.';
        }

      });
  }



  // crea las letras que se muestran como placeholder cuando no hay imagen del producto
  // intenta usar como maximo las primeras 2 iniciales del nombre
  iniciales(nombre: string): string {

    // trim quita espacios de los extremos
    // split separa el nombre cada vez que encuentra uno o mas espacios
    // filter(Boolean) elimina cualquier parte vacia que haya quedado
    const palabras = nombre
      .trim()
      .split(/\s+/)
      .filter(Boolean);

    // si por alguna razon el producto no tiene nombre usa PR
    if (!palabras.length) {
      return 'PR';
    }

    // si solamente tiene una palabra agarra las primeras 2 letras
    // slice(0, 2) significa desde la posicion 0 hasta antes de la posicion 2
    if (palabras.length === 1) {
      return palabras[0]
        .slice(0, 2)
        .toUpperCase();
    }

    // si tiene 2 o mas palabras agarra la primera letra de las primeras 2
    // toUpperCase las convierte a mayusculas
    return `${palabras[0][0]}${palabras[1][0]}`
      .toUpperCase();
  }



  // limpia cualquier mensaje anterior antes de hacer una nueva accion
  private limpiarMensajes(): void {
    this.error = '';
    this.mensaje = '';
  }

}
