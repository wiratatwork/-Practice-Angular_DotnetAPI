import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { NgIcon } from '@ng-icons/core';
import { AuthService } from '../auth/auth.service';
import { APP_ICONS, AppIconName } from '../shared/app-icons';

interface NavItem {
    label: string;
    icon: AppIconName;
    route: string;
}

const SIDEBAR_COLLAPSED_KEY = 'app-sidebar-collapsed';

@Component({
    selector: 'app-shell',
    standalone: true,
    imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive, NgIcon],
    templateUrl: './app-shell.component.html',
    styleUrl: './app-shell.component.css',
})
export class AppShellComponent {
    private readonly authService = inject(AuthService);

    readonly currentUser = this.authService.currentUser;
    readonly isAdmin = this.authService.isAdmin;
    readonly icons = APP_ICONS;

    readonly navItems: NavItem[] = [
        { label: 'หน้าหลัก', icon: APP_ICONS.navHome, route: '/' },
        { label: 'Machine', icon: APP_ICONS.navMachine, route: '/machine' },
    ];

    readonly sidebarCollapsed = signal(this.readCollapsedPreference());

    toggleSidebar(): void {
        const next = !this.sidebarCollapsed();
        this.sidebarCollapsed.set(next);
        localStorage.setItem(SIDEBAR_COLLAPSED_KEY, String(next));
    }

    logout(): void {
        this.authService.logout();
    }

    private readCollapsedPreference(): boolean {
        try {
            return localStorage.getItem(SIDEBAR_COLLAPSED_KEY) === 'true';
        } catch {
            return false;
        }
    }
}
