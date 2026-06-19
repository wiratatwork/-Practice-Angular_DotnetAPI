import { routes } from './app.routes';

describe('app routes', () => {
  it('should expose login as a public route', () => {
    const loginRoute = routes.find((route) => route.path === 'login');
    expect(loginRoute).toBeTruthy();
    expect(loginRoute?.canActivate).toBeUndefined();
  });

  it('should protect the shell route and define child routes', () => {
    const shellRoute = routes.find((route) => route.path === '');
    expect(shellRoute?.canActivate).toBeTruthy();
    expect(shellRoute?.children?.map((child) => child.path)).toEqual(['', 'machine']);
  });

  it('should redirect unknown paths to the app entry route', () => {
    const wildcardRoute = routes.find((route) => route.path === '**');
    expect(wildcardRoute?.redirectTo).toBe('');
  });
});
