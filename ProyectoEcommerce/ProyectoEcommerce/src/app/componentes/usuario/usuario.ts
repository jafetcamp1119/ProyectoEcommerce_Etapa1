import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { finalize } from 'rxjs';

import { IRol, IUsuario } from '../../model/IUsuario';
import { UsuarioService } from '../../services/usuario';


// esta pantalla permite al Administrador buscar usuarios
// cambiarles el rol y activar o desactivar sus cuentas
@Component({
  selector: 'app-usuario',
  imports: [
    ReactiveFormsModule
  ],
  templateUrl: './usuario.html',
  styleUrl: './usuario.css'
})
export class Usuario implements OnInit {

  // ========================= SERVICIOS =========================

  // sirve para crear el formulario reactivo de filtros
  private readonly fb =
    inject(FormBuilder);

  // servicio que maneja usuarios y roles
  private readonly servicio =
    inject(UsuarioService);

  // se usa para pedirle a Angular que actualice la pantalla
  private readonly cdr =
    inject(ChangeDetectorRef);


  // ========================= DATOS DE LA PANTALLA =========================

  // usuarios que se muestran en la pagina actual
  usuarios: IUsuario[] = [];


  // roles disponibles para asignar
  roles: IRol[] = [];


  // guarda temporalmente el rol escogido para cada usuario
  // la llave es usuarioId y el valor es rolId
  rolesSeleccionados: Record<number, number> = {};


  // controla si se estan cargando los usuarios
  cargando = true;


  // guarda el id del usuario que se esta modificando
  // 0 significa que no hay ningun cambio procesandose
  guardandoId = 0;


  // mensajes que aparecen en pantalla
  mensaje = '';
  error = '';


  // ========================= PAGINACION =========================

  pagina = 1;

  tamanoPagina = 25;

  total = 0;


  // tamaños permitidos para mostrar registros
  readonly tamanos = [
    25,
    50,
    75,
    100
  ];


  // ========================= FILTROS =========================

  // formulario usado para buscar por texto rol o estado
  readonly filtros =
    this.fb.nonNullable.group({

      texto: '',

      rolId: '',

      estado: ''

    });


  // calcula cuantas paginas existen segun el total de registros
  get totalPaginas(): number {

    // Math.ceil redondea hacia arriba
    // Math.max evita que el total de paginas llegue a 0
    return Math.max(
      1,
      Math.ceil(
        this.total / this.tamanoPagina
      )
    );
  }



  // ngOnInit se ejecuta automaticamente al abrir la pantalla
  // primero trae los roles disponibles y tambien carga los usuarios
  ngOnInit(): void {

    // ========================= CARGAR ROLES =========================

    this.servicio
      .listarRoles()
      .subscribe({

        next: r => {

          // si data viene null o undefined usa una lista vacia
          this.roles =
            r.data ?? [];

          this.cdr.markForCheck();

        },


        error: e => {

          this.error =
            this.mensajeError(e);

          this.cdr.markForCheck();

        }

      });


    // carga la primera pagina de usuarios
    this.cargar();
  }


  // ==================== CARGAR USUARIOS =====================
 

  // arma los filtros actuales y pide la pagina correspondiente a la API
  cargar(): void {

    // agarra todos los valores actuales del formulario
    const f =
      this.filtros.getRawValue();


    this.cargando = true;

    this.error = '';


    this.servicio
      .listarAdministracion({

        // trim quita espacios al inicio y al final
        // si queda vacio manda undefined
        texto:
          f.texto.trim() || undefined,


        // si existe rolId lo convierte a numero
        rolId:
          f.rolId
            ? Number(f.rolId)
            : undefined,


        // vacio significa todos
        // activo significa true
        // cualquier otro valor queda false
        activo:
          f.estado === ''
            ? undefined
            : f.estado === 'activo',


        pagina:
          this.pagina,


        tamanoPagina:
          this.tamanoPagina

      })

      // finalize se ejecuta cuando termina la solicitud
      // tanto si sale bien como si ocurre un error
      .pipe(
        finalize(() => {

          this.cargando = false;

          this.cdr.markForCheck();

        })
      )

      .subscribe({

        next: r => {

          // guarda los usuarios de la pagina actual
          this.usuarios =
            r.data?.items ?? [];


          // guarda el total general de registros
          this.total =
            r.data?.total ?? 0;


          // crea un objeto donde cada usuarioId queda asociado
          // con el rol que tiene actualmente
          this.rolesSeleccionados =
            Object.fromEntries(
              this.usuarios.map(
                x => [
                  x.usuarioId,
                  x.rolId
                ]
              )
            );

        },


        error: e => {

          this.error =
            this.mensajeError(e);

        }

      });
  }


  // 
  // ========================= BUSCAR =========================
  // 

  // cuando se aplican filtros vuelve a la primera pagina
  buscar(): void {

    this.pagina = 1;

    this.cargar();
  }


  // ==================== LIMPIAR FILTROS =====================

  // devuelve todos los filtros a sus valores iniciales
  limpiar(): void {

    this.filtros.reset({

      texto: '',

      rolId: '',

      estado: ''

    });


    this.pagina = 1;

    this.cargar();
  }


  // ================== SELECCIONAR ROL =======================

  // guarda en memoria el nuevo rol escogido
  // todavia no lo manda a la API hasta presionar Guardar
  seleccionarRol(
    usuarioId: number,
    valor: string
  ): void {

    this.rolesSeleccionados[usuarioId] =
      Number(valor);
  }


  // ===================== GUARDAR ROL ========================

  // manda a la API el rol seleccionado para este usuario
  guardarRol(
    usuario: IUsuario
  ): void {

    // busca el rol que se escogio en el select de este usuario
    const rolId =
      this.rolesSeleccionados[usuario.usuarioId];


    // no continua si:
    // no existe rol
    // es el mismo que ya tenia
    // o ya hay otro cambio procesandose
    if (
      !rolId ||
      rolId === usuario.rolId ||
      this.guardandoId
    ) {
      return;
    }


    // guarda cual usuario se esta modificando
    this.guardandoId =
      usuario.usuarioId;


    this.limpiarMensajes();


    // manda usuarioId y rolId al backend
    this.servicio
      .cambiarRol(
        usuario.usuarioId,
        rolId
      )

      .pipe(
        finalize(() => {

          // libera nuevamente las acciones
          this.guardandoId = 0;

          this.cdr.markForCheck();

        })
      )

      .subscribe({

        next: () => {

          this.mensaje =
            'Rol actualizado correctamente.';


          // vuelve a consultar para mostrar
          // lo que realmente quedo guardado en la API
          this.cargar();

        },


        error: e => {

          // si falla vuelve a poner el rol que tenia originalmente
          this.rolesSeleccionados[usuario.usuarioId] =
            usuario.rolId;


          this.error =
            this.mensajeError(e);

        }

      });
  }


  // ==================== CAMBIAR ESTADO ======================
  

  // activa o desactiva una cuenta
  // la proteccion del ultimo Administrador queda del lado de la API
  cambiarEstado(
    usuario: IUsuario
  ): void {

    // evita mandar otra accion mientras ya se esta guardando una
    if (this.guardandoId) {
      return;
    }


    this.guardandoId =
      usuario.usuarioId;


    this.limpiarMensajes();


    // manda el estado contrario al que tiene actualmente
    this.servicio
      .cambiarEstado(
        usuario.usuarioId,
        !usuario.activo
      )

      .pipe(
        finalize(() => {

          this.guardandoId = 0;

          this.cdr.markForCheck();

        })
      )

      .subscribe({

        next: () => {

          // el mensaje depende del estado que tenia antes del cambio
          this.mensaje =
            usuario.activo
              ? 'Usuario desactivado.'
              : 'Usuario activado.';


          // vuelve a traer la lista actualizada
          this.cargar();

        },


        error: e => {

          this.error =
            this.mensajeError(e);

        }

      });
  }


  // ================== TAMAÑO DE PAGINA ======================

  cambiarTamano(
    valor: string
  ): void {

    // convierte el valor del select a numero
    this.tamanoPagina =
      Number(valor);


    // cuando cambia el tamaño vuelve a la primera pagina
    this.pagina = 1;


    this.cargar();
  }


  // ==================== CAMBIAR PAGINA ======================

  irPagina(
    delta: number
  ): void {

    // suma o resta una pagina
    // Math.max evita bajar de 1
    // Math.min evita pasar de la ultima pagina
    this.pagina =
      Math.min(
        this.totalPaginas,
        Math.max(
          1,
          this.pagina + delta
        )
      );


    this.cargar();
  }


  // =========================================================
  // =================== LIMPIAR MENSAJES =====================
  // =========================================================

  private limpiarMensajes(): void {

    this.mensaje = '';

    this.error = '';
  }


  // =================== mENSAJE DE ERROR =====================

  // intenta mostrar primero el mensaje que mande la API
  // si no viene ninguno usa un mensaje general
  private mensajeError(
    error: any
  ): string {

    return error?.error?.error ||
      error?.error?.title ||
      'No fue posible completar la operación.';
  }

}
