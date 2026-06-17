export interface LoginRequest {
    username: string;
    password: string;
}

export interface LoginResponse {
    token: string;
    username: string;
    role: string;
}

export type UserRole = 'Admin' | 'User';

export interface AuthUser {
    username: string;
    role: UserRole;
}
