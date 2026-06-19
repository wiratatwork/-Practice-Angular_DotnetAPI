import { Component, inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { NgIcon } from '@ng-icons/core';
import { AuthService } from '../auth.service';
import { APP_ICONS } from '../../shared/app-icons';

@Component({
    selector: 'app-login',
    standalone: true,
    imports: [CommonModule, ReactiveFormsModule, NgIcon],
    templateUrl: './login.component.html',
})
export class LoginComponent {
    private readonly fb = inject(FormBuilder);
    private readonly authService = inject(AuthService);
    private readonly router = inject(Router);

    readonly icons = APP_ICONS;

    form: FormGroup = this.fb.group({
        username: ['', [Validators.required, Validators.maxLength(50)]],
        password: ['', [Validators.required, Validators.maxLength(100)]],
    });

    isSubmitting = signal(false);
    errorMessage = signal<string | null>(null);

    constructor() {
        if (this.authService.isLoggedIn()) {
            this.router.navigate(['/']);
            return;
        }

        this.authService.initializeSession().subscribe((success) => {
            if (success) {
                this.router.navigate(['/']);
            }
        });
    }

    submit(): void {
        this.form.markAllAsTouched();

        if (this.form.invalid) {
            return;
        };
        
        this.isSubmitting.set(true);
        this.errorMessage.set(null);

        const credentials = {
            username: this.form.get('username')!.value?.trim(),
            password: this.form.get('password')!.value,
        };

        this.authService.login(credentials).subscribe({
            next: () => {
                this.isSubmitting.set(false);
                this.router.navigate(['/']);
            },
            error: (err) => {
                this.isSubmitting.set(false);
                const msg = err?.error?.message ?? 'ไม่สามารถเข้าสู่ระบบได้ กรุณาลองใหม่';
                this.errorMessage.set(msg);
            },
        });
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
