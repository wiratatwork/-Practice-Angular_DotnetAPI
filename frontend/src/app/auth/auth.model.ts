export interface LoginRequest {
  username: string;
  password: string;
}

export interface TokenResponse {
  accessToken: string;
  expiresIn: number;
  user: AuthUser;
}

export type LoginResponse = TokenResponse;
export type RefreshResponse = TokenResponse;

export type UserRole = 'Admin' | 'User';

export interface AuthUser {
  username: string;
  role: UserRole;
}
