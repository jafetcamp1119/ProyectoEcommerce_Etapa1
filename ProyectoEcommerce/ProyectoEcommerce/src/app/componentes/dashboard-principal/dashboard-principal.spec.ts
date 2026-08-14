import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { DashboardPrincipal } from './dashboard-principal';


// este archivo prueba que el componente DashboardPrincipal pueda crearse correctamente
describe('DashboardPrincipal', () => {

  // fixture representa el componente junto con su HTML dentro del ambiente de prueba
  let fixture: ComponentFixture<DashboardPrincipal>;

  // aqui se guarda la instancia real del componente que se va a probar
  let component: DashboardPrincipal;


  // beforeEach se ejecuta antes de cada prueba que haya dentro de este describe
  beforeEach(async () => {

    // TestBed prepara un ambiente parecido al que Angular usa normalmente
    // para poder crear y probar el componente sin abrir toda la aplicacion
    await TestBed.configureTestingModule({

      // carga el componente que queremos probar
      imports: [DashboardPrincipal],

      // agrega un Router vacio porque el componente necesita las herramientas de rutas
      providers: [provideRouter([])]

    })
      // compileComponents termina de preparar el componente y su plantilla para las pruebas
      .compileComponents();


    // crea una instancia del DashboardPrincipal dentro del ambiente de prueba
    fixture = TestBed.createComponent(DashboardPrincipal);

    // componentInstance agarra directamente la clase del componente creado
    component = fixture.componentInstance;

    // detectChanges hace que Angular procese el componente y actualice su vista
    fixture.detectChanges();

  });


  // esta prueba solamente revisa que el componente se pueda crear sin errores
  it('should create', () => {

    // toBeTruthy espera que component exista y tenga un valor valido
    expect(component).toBeTruthy();

  });

});
