import { Component, inject, OnInit, signal, DestroyRef } from '@angular/core';
import {
    FormBuilder,
    FormGroup,
    ReactiveFormsModule,
    Validators,
} from '@angular/forms';
import { CommonModule } from '@angular/common';
import { NgIcon } from '@ng-icons/core';
import { MachineService } from './machine.service';
import { Machine } from './machine.model';
import { debounceTime, distinctUntilChanged, switchMap, of, Subject } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../auth/auth.service';
import { APP_ICONS } from '../shared/app-icons';

@Component({
    selector: 'app-machine',
    standalone: true,
    imports: [CommonModule, ReactiveFormsModule, NgIcon],
    templateUrl: './machine.component.html',
    styleUrl: './machine.component.css',
})
export class MachineComponent implements OnInit {
    private readonly fb = inject(FormBuilder);
    private readonly machineService = inject(MachineService);
    private readonly authService = inject(AuthService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly searchSubject = new Subject<string>();

    readonly isAdmin = this.authService.isAdmin;
    readonly icons = APP_ICONS;

    machines = signal<Machine[]>([]);
    isLoading = signal(false);
    isSearching = signal(false);
    searchTerm = signal('');
    isModalOpen = signal(false);
    isEditMode = signal(false);
    editingMachineNo = signal<string | null>(null);
    isSubmitting = signal(false);
    deleteConfirmMachineNo = signal<string | null>(null);
    errorMessage = signal<string | null>(null);
    successMessage = signal<string | null>(null);
    isDuplicateNameChecking = signal(false);

    form!: FormGroup;

    ngOnInit(): void {
        this.initForm();
        this.loadMachines();
        this.initSearch();
    }

    private initSearch(): void {
        this.searchSubject.pipe(
            debounceTime(300),
            distinctUntilChanged(),
            switchMap((term) => {
                if (!term.trim()) {
                    this.isSearching.set(false);
                    this.loadMachines();
                    return of(null);
                }
                this.isSearching.set(true);
                return this.machineService.search(term);
            }),
            takeUntilDestroyed(this.destroyRef),
        ).subscribe({
            next: (data) => {
                if (data !== null) {
                    this.machines.set(data);
                    this.isSearching.set(false);
                }
            },
            error: () => {
                this.isSearching.set(false);
                this.showError('ไม่สามารถค้นหาได้ กรุณาลองใหม่');
            },
        });
    }

    onSearchInput(event: Event): void {
        const value = (event.target as HTMLInputElement).value;
        this.searchTerm.set(value);
        this.searchSubject.next(value);
    }

    clearSearch(): void {
        this.searchTerm.set('');
        this.searchSubject.next('');
    }

    private initForm(): void {
        this.form = this.fb.group({
            machineNo: ['', [Validators.required, Validators.maxLength(50)]],
            machineName: ['', [Validators.required, Validators.maxLength(50)]],
            plant: ['', [Validators.required, Validators.maxLength(10)]],
            status: ['', [Validators.required, Validators.maxLength(10)]],
        });
    }

    loadMachines(): void {
        this.isLoading.set(true);
        this.machineService.getAll().subscribe({
            next: (data) => {
                this.machines.set(data);
                this.isLoading.set(false);
            },
            error: () => {
                this.showError('ไม่สามารถโหลดข้อมูลได้ กรุณาลองใหม่อีกครั้ง');
                this.isLoading.set(false);
            },
        });
    }

    openAddModal(): void {
        this.isEditMode.set(false);
        this.editingMachineNo.set(null);
        this.form.reset();
        this.form.get('machineNo')?.enable();
        this.isModalOpen.set(true);
        this.errorMessage.set(null);
    }

    openEditModal(machine: Machine): void {
        this.isEditMode.set(true);
        this.editingMachineNo.set(machine.machineNo);
        this.form.patchValue({
            machineNo: machine.machineNo,
            machineName: machine.machineName,
            plant: machine.plant,
            status: machine.status,
        });
        this.form.get('machineNo')?.disable();
        this.isModalOpen.set(true);
        this.errorMessage.set(null);
    }

    closeModal(): void {
        this.isModalOpen.set(false);
        this.form.reset();
        this.form.get('machineNo')?.enable();
        this.errorMessage.set(null);
    }

    submitForm(): void {
        this.form.markAllAsTouched();

        if (this.form.invalid) {
            return;
        }

        const machineName = this.form.get('machineName')!.value?.trim();
        const excludeMachineNo = this.isEditMode()
            ? this.editingMachineNo() ?? undefined
            : undefined;

        this.isDuplicateNameChecking.set(true);
        this.machineService.checkDuplicateName(machineName, excludeMachineNo).subscribe({
            next: (result) => {
                this.isDuplicateNameChecking.set(false);
                if (result.isDuplicate) {
                    this.form.get('machineName')!.setErrors({ duplicateName: true });
                    return;
                }
                this.saveForm();
            },
            error: () => {
                this.isDuplicateNameChecking.set(false);
                this.showError('ไม่สามารถตรวจสอบชื่อซ้ำได้ กรุณาลองใหม่');
            },
        });
    }

    private saveForm(): void {
        this.isSubmitting.set(true);
        this.errorMessage.set(null);

        if (this.isEditMode()) {
            const dto = {
                machineName: this.form.get('machineName')!.value?.trim(),
                plant: this.form.get('plant')!.value?.trim(),
                status: this.form.get('status')!.value?.trim(),
            };
            this.machineService.update(this.editingMachineNo()!, dto).subscribe({
                next: () => {
                    this.isSubmitting.set(false);
                    this.closeModal();
                    this.loadMachines();
                    this.showSuccess('แก้ไขข้อมูลเครื่องจักรสำเร็จ');
                },
                error: (err) => {
                    this.isSubmitting.set(false);
                    const msg = err?.error?.message ?? 'เกิดข้อผิดพลาด กรุณาลองใหม่';
                    this.showError(msg);
                },
            });
        } else {
            const dto = {
                machineNo: this.form.get('machineNo')!.value?.trim(),
                machineName: this.form.get('machineName')!.value?.trim(),
                plant: this.form.get('plant')!.value?.trim(),
                status: this.form.get('status')!.value?.trim(),
            };
            this.machineService.create(dto).subscribe({
                next: () => {
                    this.isSubmitting.set(false);
                    this.closeModal();
                    this.loadMachines();
                    this.showSuccess('เพิ่มเครื่องจักรสำเร็จ');
                },
                error: (err) => {
                    this.isSubmitting.set(false);
                    if (err?.error?.code === 'MACHINE_NO_DUPLICATE') {
                        this.form.get('machineNo')!.setErrors({ duplicateMachineNo: true });
                    } else {
                        const msg = err?.error?.message ?? 'เกิดข้อผิดพลาด กรุณาลองใหม่';
                        this.showError(msg);
                    }
                },
            });
        }
    }

    confirmDelete(machineNo: string): void {
        this.deleteConfirmMachineNo.set(machineNo);
    }

    cancelDelete(): void {
        this.deleteConfirmMachineNo.set(null);
    }

    deleteMachine(): void {
        const machineNo = this.deleteConfirmMachineNo();
        if (!machineNo) return;

        this.machineService.delete(machineNo).subscribe({
            next: () => {
                this.deleteConfirmMachineNo.set(null);
                this.loadMachines();
                this.showSuccess('ลบเครื่องจักรสำเร็จ');
            },
            error: () => {
                this.deleteConfirmMachineNo.set(null);
                this.showError('ไม่สามารถลบเครื่องจักรได้ กรุณาลองใหม่');
            },
        });
    }

    getDeleteConfirmMachine(): Machine | undefined {
        const no = this.deleteConfirmMachineNo();
        return this.machines().find((m) => m.machineNo === no);
    }

    private showError(msg: string): void {
        this.errorMessage.set(msg);
        this.successMessage.set(null);
        setTimeout(() => this.errorMessage.set(null), 5000);
    }

    private showSuccess(msg: string): void {
        this.successMessage.set(msg);
        this.errorMessage.set(null);
        setTimeout(() => this.successMessage.set(null), 4000);
    }

    hasError(controlName: string, errorCode: string): boolean {
        const ctrl = this.form.get(controlName);
        return !!(ctrl?.hasError(errorCode) && (ctrl.dirty || ctrl.touched));
    }

    isInvalid(controlName: string): boolean {
        const ctrl = this.form.get(controlName);
        return !!(ctrl?.invalid && (ctrl.dirty || ctrl.touched));
    }

}
