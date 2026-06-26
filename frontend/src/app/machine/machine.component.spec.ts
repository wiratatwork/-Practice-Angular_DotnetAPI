import { TestBed } from '@angular/core/testing';
import { provideIcons } from '@ng-icons/core';
import { signal } from '@angular/core';
import { of, throwError } from 'rxjs';
import { MachineComponent } from './machine.component';
import { MachineService } from './machine.service';
import { AuthService } from '../auth/auth.service';
import { APP_ICON_REGISTRY } from '../shared/app-icons';
import { Machine } from './machine.model';

describe('MachineComponent', () => {
  const sampleMachine: Machine = {
    machineNo: 'M001',
    machineName: 'Press A',
    plant: 'P1',
    status: 'ONLINE',
  };

  let machineServiceMock: {
    getAll: ReturnType<typeof vi.fn>;
    search: ReturnType<typeof vi.fn>;
    checkDuplicateName: ReturnType<typeof vi.fn>;
    create: ReturnType<typeof vi.fn>;
    update: ReturnType<typeof vi.fn>;
    delete: ReturnType<typeof vi.fn>;
  };

  let isAdminSignal: ReturnType<typeof signal<boolean>>;

  function setup(isAdmin = true) {
    isAdminSignal = signal(isAdmin);

    machineServiceMock = {
      getAll: vi.fn(() => of([sampleMachine])),
      search: vi.fn(() => of([sampleMachine])),
      checkDuplicateName: vi.fn(() => of({ isDuplicate: false })),
      create: vi.fn(() => of(undefined)),
      update: vi.fn(() => of(undefined)),
      delete: vi.fn(() => of(undefined)),
    };

    TestBed.configureTestingModule({
      imports: [MachineComponent],
      providers: [
        provideIcons(APP_ICON_REGISTRY),
        {
          provide: AuthService,
          useValue: {
            isAdmin: isAdminSignal.asReadonly(),
          },
        },
        { provide: MachineService, useValue: machineServiceMock },
      ],
    });

    const fixture = TestBed.createComponent(MachineComponent);
    fixture.detectChanges();
    return fixture;
  }

  it('should create the machine component', () => {
    const fixture = setup();
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should load machines on init', () => {
    const fixture = setup();
    expect(machineServiceMock.getAll).toHaveBeenCalled();
    expect(fixture.componentInstance.machines()).toEqual([sampleMachine]);
    expect(fixture.componentInstance.isLoading()).toBe(false);
  });

  it('should show load error when getAll fails', () => {
    machineServiceMock = {
      getAll: vi.fn(() => throwError(() => new Error('fail'))),
      search: vi.fn(() => of([])),
      checkDuplicateName: vi.fn(() => of({ isDuplicate: false })),
      create: vi.fn(() => of(undefined)),
      update: vi.fn(() => of(undefined)),
      delete: vi.fn(() => of(undefined)),
    };

    TestBed.configureTestingModule({
      imports: [MachineComponent],
      providers: [
        provideIcons(APP_ICON_REGISTRY),
        {
          provide: AuthService,
          useValue: { isAdmin: signal(true).asReadonly() },
        },
        { provide: MachineService, useValue: machineServiceMock },
      ],
    });

    const fixture = TestBed.createComponent(MachineComponent);
    fixture.detectChanges();

    expect(fixture.componentInstance.errorMessage()).toBe(
      'ไม่สามารถโหลดข้อมูลได้ กรุณาลองใหม่อีกครั้ง',
    );
  });

  it('should show add button for admin users', () => {
    const fixture = setup(true);
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.btn.btn-primary')?.textContent).toContain('เพิ่มเครื่องจักร');
    expect(compiled.querySelector('.actions-cell')).toBeTruthy();
  });

  it('should hide admin actions for non-admin users', () => {
    const fixture = setup(false);
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.btn.btn-primary')).toBeNull();
    expect(compiled.querySelector('.actions-cell')).toBeNull();
  });

  it('should not submit when add form is invalid', () => {
    const fixture = setup();
    fixture.componentInstance.openAddModal();
    fixture.detectChanges();

    fixture.componentInstance.submitForm();

    expect(machineServiceMock.checkDuplicateName).not.toHaveBeenCalled();
    expect(machineServiceMock.create).not.toHaveBeenCalled();
  });

  it('should block save when duplicate name is detected', () => {
    const fixture = setup();
    machineServiceMock.checkDuplicateName.mockReturnValue(of({ isDuplicate: true }));

    fixture.componentInstance.openAddModal();
    fixture.componentInstance.form.setValue({
      machineNo: 'M002',
      machineName: 'Press A',
      plant: 'P1',
      status: 'ONLINE',
    });
    fixture.componentInstance.submitForm();

    expect(machineServiceMock.checkDuplicateName).toHaveBeenCalledWith('Press A', undefined);
    expect(machineServiceMock.create).not.toHaveBeenCalled();
    expect(fixture.componentInstance.form.get('machineName')?.hasError('duplicateName')).toBe(true);
  });

  it('should create machine successfully', () => {
    const fixture = setup();
    fixture.componentInstance.openAddModal();
    fixture.componentInstance.form.setValue({
      machineNo: 'M002',
      machineName: 'Press B',
      plant: 'P1',
      status: 'ONLINE',
    });
    fixture.componentInstance.submitForm();

    expect(machineServiceMock.create).toHaveBeenCalledWith({
      machineNo: 'M002',
      machineName: 'Press B',
      plant: 'P1',
      status: 'ONLINE',
    });
    expect(fixture.componentInstance.isModalOpen()).toBe(false);
    expect(fixture.componentInstance.successMessage()).toBe('เพิ่มเครื่องจักรสำเร็จ');
    expect(machineServiceMock.getAll).toHaveBeenCalledTimes(2);
  });

  it('should debounce search input', () => {
    vi.useFakeTimers();
    const fixture = setup();
    machineServiceMock.getAll.mockClear();

    const input = fixture.nativeElement.querySelector('.search-input') as HTMLInputElement;
    input.value = 'Press';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(machineServiceMock.search).not.toHaveBeenCalled();

    vi.advanceTimersByTime(300);
    fixture.detectChanges();

    expect(machineServiceMock.search).toHaveBeenCalledWith('Press');
    vi.useRealTimers();
  });

  it('should reload machines when search is cleared', () => {
    vi.useFakeTimers();
    const fixture = setup();
    machineServiceMock.getAll.mockClear();

    fixture.componentInstance.onSearchInput({ target: { value: 'Press' } } as unknown as Event);
    vi.advanceTimersByTime(300);
    fixture.detectChanges();

    machineServiceMock.getAll.mockClear();
    fixture.componentInstance.clearSearch();
    vi.advanceTimersByTime(300);
    fixture.detectChanges();

    expect(fixture.componentInstance.searchTerm()).toBe('');
    expect(machineServiceMock.getAll).toHaveBeenCalled();
    vi.useRealTimers();
  });

  it('should disable machineNo in edit mode and call update on save', () => {
    const fixture = setup();
    fixture.componentInstance.openEditModal(sampleMachine);
    fixture.detectChanges();

    expect(fixture.componentInstance.form.get('machineNo')?.disabled).toBe(true);

    fixture.componentInstance.form.patchValue({
      machineName: 'Press Updated',
      plant: 'P2',
      status: 'OFFLINE',
    });
    fixture.componentInstance.submitForm();

    expect(machineServiceMock.checkDuplicateName).toHaveBeenCalledWith('Press Updated', 'M001');
    expect(machineServiceMock.update).toHaveBeenCalledWith('M001', {
      machineName: 'Press Updated',
      plant: 'P2',
      status: 'OFFLINE',
    });
    expect(fixture.componentInstance.successMessage()).toBe('แก้ไขข้อมูลเครื่องจักรสำเร็จ');
  });

  it('should show delete confirmation and delete machine on confirm', () => {
    const fixture = setup();
    const compiled = fixture.nativeElement as HTMLElement;

    fixture.componentInstance.confirmDelete('M001');
    fixture.detectChanges();

    expect(compiled.textContent).toContain('Press A');
    expect(compiled.textContent).toContain('M001');

    fixture.componentInstance.deleteMachine();
    fixture.detectChanges();

    expect(machineServiceMock.delete).toHaveBeenCalledWith('M001');
    expect(fixture.componentInstance.deleteConfirmMachineNo()).toBeNull();
    expect(fixture.componentInstance.successMessage()).toBe('ลบเครื่องจักรสำเร็จ');
  });

  it('should show delete error when delete fails', () => {
    const fixture = setup();
    machineServiceMock.delete.mockReturnValue(throwError(() => new Error('fail')));

    fixture.componentInstance.confirmDelete('M001');
    fixture.componentInstance.deleteMachine();
    fixture.detectChanges();

    expect(fixture.componentInstance.errorMessage()).toBe(
      'ไม่สามารถลบเครื่องจักรได้ กรุณาลองใหม่',
    );
    expect(fixture.componentInstance.deleteConfirmMachineNo()).toBeNull();
  });
});
