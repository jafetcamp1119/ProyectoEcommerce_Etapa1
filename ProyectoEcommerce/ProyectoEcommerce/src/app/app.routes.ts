import { Routes } from '@angular/router';

import { AccesoBloqueado } from './componentes/acceso-bloqueado/acceso-bloqueado';
import { Autenticacion } from './componentes/autenticacion/autenticacion';
import { Carrito } from './componentes/carrito/carrito';
import { Categoria } from './componentes/categoria/categoria';
import { CategoriasCliente } from './componentes/categorias-cliente/categorias-cliente';
import { Checkout } from './componentes/checkout/checkout';
import { ConfiguracionInicial } from './componentes/configuracion-inicial/configuracion-inicial';
import { DashboardPrincipal } from './componentes/dashboard-principal/dashboard-principal';
import { Descuento } from './componentes/descuento/descuento';
import { FamiliaProducto } from './componentes/familia-producto/familia-producto';
import { Impuesto } from './componentes/impuesto/impuesto';
import { Orden } from './componentes/orden/orden';
import { OrdenDetalle } from './componentes/orden-detalle/orden-detalle';
import { Principal } from './componentes/principal/principal';
import { Producto } from './componentes/producto/producto';
import { ProductoDetalle } from './componentes/producto-detalle/producto-detalle';
import { Usuario } from './componentes/usuario/usuario';
import { ProveedoresInicio } from './componentes/proveedores-inicio/proveedores-inicio';
import { ProveedorGestion } from './componentes/proveedor-gestion/proveedor-gestion';
import { CatalogoProveedor } from './componentes/catalogo-proveedor/catalogo-proveedor';
import { CompraProveedor } from './componentes/compra-proveedor/compra-proveedor';
import { HistorialComprasProveedor } from './componentes/historial-compras-proveedor/historial-compras-proveedor';

import { adminGuard } from './guards/admin.guard';
import { authGuard } from './guards/auth.guard';
import { clienteGuard } from './guards/cliente.guard';
import {
  configuracionCompletaGuard,
  configuracionDisponibleGuard
} from './guards/configuracion-inicial.guard';


export const routes: Routes = [

  // ========================= RUTAS PUBLICAS =========================

  {
    path: 'configuracion-inicial',
    component: ConfiguracionInicial,
    canActivate: [configuracionDisponibleGuard]
  },

  {
    path: 'auth',
    component: Autenticacion,
    canActivate: [configuracionCompletaGuard]
  },

  {
    path: 'acceso-bloqueado',
    component: AccesoBloqueado,
    canActivate: [configuracionCompletaGuard]
  },


  // ========================= DASBOARD =========================

  // children hace que estas pantallas se carguen dentro del DashboardPrincipal
  {
    path: '',
    component: DashboardPrincipal,
    canActivate: [configuracionCompletaGuard, authGuard],

    children: [

      {
        path: '',
        component: Principal
      },


      // ========================= ADMINISTRADOR =========================

      {
        path: 'familias-producto',
        component: FamiliaProducto,
        canActivate: [adminGuard]
      },

      {
        path: 'categorias/:familiaId',
        component: Categoria,
        canActivate: [adminGuard]
      },

      {
        path: 'categorias',
        component: Categoria,
        canActivate: [adminGuard]
      },

      {
        path: 'impuestos',
        component: Impuesto,
        canActivate: [adminGuard]
      },

      {
        path: 'descuentos',
        component: Descuento,
        canActivate: [adminGuard]
      },

      {
        path: 'roles',
        component: Usuario,
        canActivate: [adminGuard]
      },

      {
        path: 'usuarios',
        redirectTo: 'roles'
      },

      {
        path: 'proveedores',
        component: ProveedoresInicio,
        canActivate: [adminGuard]
      },

      {
        path: 'proveedores/gestion',
        component: ProveedorGestion,
        canActivate: [adminGuard]
      },

      {
        path: 'proveedores/catalogo/:proveedorId/categoria/:categoriaId',
        component: CatalogoProveedor,
        canActivate: [adminGuard]
      },

      {
        path: 'proveedores/catalogo/:proveedorId',
        component: CatalogoProveedor,
        canActivate: [adminGuard]
      },

      {
        path: 'proveedores/catalogo',
        component: CatalogoProveedor,
        canActivate: [adminGuard]
      },

      {
        path: 'proveedores/compras/:proveedorId',
        component: CompraProveedor,
        canActivate: [adminGuard]
      },

      {
        path: 'proveedores/compras',
        component: CompraProveedor,
        canActivate: [adminGuard]
      },

      {
        path: 'proveedores/historial',
        component: HistorialComprasProveedor,
        canActivate: [adminGuard]
      },


      // ========================= PRODUCTOS =========================

      {
        path: 'productos',
        component: Producto
      },

      // data manda informacion extra al componente para saber
      // si la busqueda es global por familia o por categoria
      {
        path: 'productos/buscar',
        component: Producto,
        canActivate: [clienteGuard],
        data: { alcanceCliente: 'global' }
      },

      {
        path: 'productos/familia/:familiaId',
        component: CategoriasCliente,
        canActivate: [clienteGuard]
      },

      {
        path: 'productos/familia/:familiaId/buscar',
        component: Producto,
        canActivate: [clienteGuard],
        data: { alcanceCliente: 'familia' }
      },

      {
        path: 'productos/familia/:familiaId/categoria/:categoriaId',
        component: Producto,
        canActivate: [clienteGuard],
        data: { alcanceCliente: 'categoria' }
      },

      {
        path: 'productos/:productoId',
        component: ProductoDetalle
      },


      // ========================= COMPRA =========================

      {
        path: 'carrito',
        component: Carrito,
        canActivate: [clienteGuard]
      },

      {
        path: 'checkout',
        component: Checkout,
        canActivate: [clienteGuard]
      },


      // ========================= ORDENES =========================

      {
        path: 'ordenes/:ordenId',
        component: OrdenDetalle
      },

      {
        path: 'ordenes',
        component: Orden
      }

    ]
  },

  // ========================= RUTA NO ENCONTRADA =========================

  // ** atrapa cualquier URL que no exista y la devuelve al inicio
  {
    path: '**',
    redirectTo: ''
  }

];
