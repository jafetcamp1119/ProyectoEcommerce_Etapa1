import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { Principal } from './principal';


// pruebas basicas del componente Principal
describe('Principal', () => {

  // fixture representa el componente dentro del ambiente de pruebas
  let fixture: ComponentFixture<Principal>;

  // component guarda la instancia real del componente
  let component: Principal;


  // beforeEach se ejecuta antes de cada prueba
  beforeEach(async () => {

    // configura el ambiente que necesita Principal para poder probarse
    await TestBed.configureTestingModule({

      // carga directamente el componente que se va a probar
      imports: [
        Principal
      ],

      // crea un Router vacio para que el componente pueda usar rutas
      // sin necesitar cargar las rutas reales de la aplicacion
      providers: [
        provideRouter([])
      ]

    })
      // compila el componente antes de crear la prueba
      .compileComponents();


    // crea Principal dentro del ambiente de pruebas
    fixture = TestBed.createComponent(Principal);

    // agarra la instancia del componente creado
    component = fixture.componentInstance;

    // ejecuta la deteccion inicial de cambios de Angular
    fixture.detectChanges();

  });


  // esta prueba revisa simplemente que Principal se pueda crear correctamente
  it('should create', () => {

    // toBeTruthy comprueba que component exista
    expect(component).toBeTruthy();

  });

});
