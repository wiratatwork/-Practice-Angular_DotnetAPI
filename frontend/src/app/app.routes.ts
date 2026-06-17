import { Routes } from '@angular/router';
import { MachineComponent } from './machine/machine.component';
import { LoginComponent } from './auth/login/login.component';
import { authGuard } from './auth/auth.guard';

export const routes: Routes = [
    { path: 'login', component: LoginComponent },
    { path: '', component: MachineComponent, canActivate: [authGuard] },
    { path: '**', redirectTo: '' },
];
