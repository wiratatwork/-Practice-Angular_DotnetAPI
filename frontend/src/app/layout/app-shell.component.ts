import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../auth/auth.service';

interface NavItem {
    label: string;
    icon: string;
    route: string;
}

const SIDEBAR_COLLAPSED_KEY = 'app-sidebar-collapsed';

@Component({
    selector: 'app-shell',
    standalone: true,
    imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
    templateUrl: './app-shell.component.html',
    styleUrl: './app-shell.component.css',
})
export class AppShellComponent {
    private readonly authService = inject(AuthService);

    readonly currentUser = this.authService.currentUser;
    readonly isAdmin = this.authService.isAdmin;

    readonly navItems: NavItem[] = [
        { label: 'หน้าหลัก', icon: '🏠', route: '/' },
        { label: 'Machine', icon: '⚙️', route: '/machine' },
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
