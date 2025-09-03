import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdminParametersComponent } from './admin-parameters.component';

describe('AdminParametersComponent', () => {
  let component: AdminParametersComponent;
  let fixture: ComponentFixture<AdminParametersComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminParametersComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AdminParametersComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});