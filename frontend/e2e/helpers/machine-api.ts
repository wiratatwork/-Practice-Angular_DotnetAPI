import type { APIRequestContext } from '@playwright/test';

const ADMIN = { username: 'admin', password: 'Admin@1234' };

export async function getAdminToken(request: APIRequestContext, baseURL: string): Promise<string> {
  const response = await request.post(`${baseURL}/api/auth/login`, { data: ADMIN });
  if (!response.ok()) {
    throw new Error(`Login failed: ${response.status()}`);
  }

  const body = await response.json();
  return body.accessToken as string;
}

export async function deleteMachine(
  request: APIRequestContext,
  baseURL: string,
  machineNo: string,
): Promise<void> {
  const token = await getAdminToken(request, baseURL);
  await request.delete(`${baseURL}/api/machine/${machineNo}`, {
    headers: { Authorization: `Bearer ${token}` },
  });
}

export async function deleteE2eMachines(
  request: APIRequestContext,
  baseURL: string,
): Promise<void> {
  const token = await getAdminToken(request, baseURL);
  const headers = { Authorization: `Bearer ${token}` };

  const response = await request.get(`${baseURL}/api/machine`, { headers });
  if (!response.ok()) {
    return;
  }

  const machines: { machineNo: string }[] = await response.json();

  await Promise.all(
    machines
      .filter((machine) => machine.machineNo.startsWith('E2E-'))
      .map((machine) => request.delete(`${baseURL}/api/machine/${machine.machineNo}`, { headers })),
  );
}
