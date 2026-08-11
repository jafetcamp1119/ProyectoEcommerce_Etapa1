import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DefaultHeader } from './default-header';

describe('DefaultHeader', () => {
  let component: DefaultHeader;
  let fixture: ComponentFixture<DefaultHeader>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DefaultHeader]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DefaultHeader);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
