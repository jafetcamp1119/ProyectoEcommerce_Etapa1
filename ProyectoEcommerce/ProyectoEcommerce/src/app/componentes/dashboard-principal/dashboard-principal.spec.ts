import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DashboardPrincipal } from './dashboard-principal';
import { provideRouter } from '@angular/router';

describe('DashboardPrincipal', () => {
  let component: DashboardPrincipal;
  let fixture: ComponentFixture<DashboardPrincipal>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DashboardPrincipal],
      providers: [provideRouter([])]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DashboardPrincipal);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
