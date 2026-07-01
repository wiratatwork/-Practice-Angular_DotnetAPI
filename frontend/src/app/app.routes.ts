import { Routes } from '@angular/router';
import { MachineComponent } from './machine/machine.component';
import { LoginComponent } from './auth/login/login.component';
import { HomeComponent } from './home/home.component';
import { AppShellComponent } from './layout/app-shell.component';
import { authGuard } from './auth/auth.guard';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  {
    path: '',
    component: AppShellComponent,
    canActivate: [authGuard],
    children: [
      { path: '', component: HomeComponent },
      { path: 'machine', component: MachineComponent },
    ],
  },
  { path: '**', redirectTo: '' },
];
