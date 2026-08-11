import { Routes } from '@angular/router';
import { DashboardPrincipal } from './componentes/dashboard-principal/dashboard-principal';
import { Principal } from './componentes/principal/principal';
import { FamiliaProducto } from './componentes/familia-producto/familia-producto';
import { Categoria } from './componentes/categoria/categoria';
import { Impuesto } from './componentes/impuesto/impuesto';
import { Producto } from './componentes/producto/producto';
import { ProductoDetalle } from './componentes/producto-detalle/producto-detalle';
import { Usuario } from './componentes/usuario/usuario';
import { Orden } from './componentes/orden/orden';
import { Autenticacion } from './componentes/autenticacion/autenticacion';
import { authGuard } from './guards/auth.guard';
import { adminGuard } from './guards/admin.guard';
import { AccesoBloqueado } from './componentes/acceso-bloqueado/acceso-bloqueado';
import { Carrito } from './componentes/carrito/carrito';
import { clienteGuard } from './guards/cliente.guard';
import { Checkout } from './componentes/checkout/checkout';
import { OrdenDetalle } from './componentes/orden-detalle/orden-detalle';
import { CategoriasCliente } from './componentes/categorias-cliente/categorias-cliente';

/**
 * Define la navegación protegida y separa las pantallas de Cliente de las administrativas.
 */
export const routes: Routes = [
  { path: 'auth', component: Autenticacion },
  { path: 'acceso-bloqueado', component: AccesoBloqueado },
  {
    path: '',
    component: DashboardPrincipal,
    canActivate: [authGuard],
    children: [
      { path: '', component: Principal },
      { path: 'familias-producto', component: FamiliaProducto, canActivate: [adminGuard] },
      { path: 'categorias/:familiaId', component: Categoria, canActivate: [adminGuard] },
      { path: 'categorias', component: Categoria, canActivate: [adminGuard] },
      { path: 'impuestos', component: Impuesto, canActivate: [adminGuard] },
      // El Cliente recorre familias → categorías → productos; las búsquedas conservan su alcance.
      { path: 'productos', component: Producto },
      { path: 'productos/buscar', component: Producto, canActivate: [clienteGuard], data: { alcanceCliente: 'global' } },
      { path: 'productos/familia/:familiaId', component: CategoriasCliente, canActivate: [clienteGuard] },
      { path: 'productos/familia/:familiaId/buscar', component: Producto, canActivate: [clienteGuard], data: { alcanceCliente: 'familia' } },
      { path: 'productos/familia/:familiaId/categoria/:categoriaId', component: Producto, canActivate: [clienteGuard], data: { alcanceCliente: 'categoria' } },
      { path: 'productos/:productoId', component: ProductoDetalle },
      { path: 'carrito', component: Carrito, canActivate: [clienteGuard] },
      { path: 'checkout', component: Checkout, canActivate: [clienteGuard] },
      { path: 'roles', component: Usuario, canActivate: [adminGuard] },
      { path: 'usuarios', redirectTo: 'roles' },
      { path: 'ordenes/:ordenId', component: OrdenDetalle },
      { path: 'ordenes', component: Orden }
    ]
  },
  { path: '**', redirectTo: '' }
];
