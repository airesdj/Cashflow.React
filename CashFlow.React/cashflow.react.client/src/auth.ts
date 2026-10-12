export type CurrentUser = { userName: string };

async function getToken(): Promise<string> {
  const response = await fetch('/api/Login/Antiforgery', { credentials: 'same-origin', cache: 'no-store' });
  if (!response.ok) throw new Error('Unable to prepare the request. Please try again.');
  const data: { token: string } = await response.json();
  return data.token;
}

export async function currentUser(): Promise<CurrentUser | null> {
  const response = await fetch('/api/Login/Me', { credentials: 'same-origin', cache: 'no-store' });
  if (response.status === 401) return null;
  if (!response.ok) throw new Error('Unable to connect to the server. Please try again.');
  return response.json();
}

export async function signIn(userName: string, password: string): Promise<CurrentUser> {
  const token = await getToken();
  const response = await fetch('/api/Login/Login', {
    method: 'POST', credentials: 'same-origin',
    headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': token },
    body: JSON.stringify({ userName, password }),
  });
  if (response.status === 401) throw new Error('Invalid username or password.');
  if (response.status === 400) throw new Error('Unable to sign in. Check your details and try again.');
  if (!response.ok) throw new Error('Unable to connect to the server. Please try again.');
  return response.json();
}

export async function signOut(): Promise<void> {
  // Obtain a fresh token for the current authenticated identity.
  const token = await getToken();
  const response = await fetch('/api/Login/LogOff', {
    method: 'POST', credentials: 'same-origin', headers: { 'X-CSRF-TOKEN': token },
  });
  if (!response.ok && response.status !== 401) throw new Error('Unable to sign out. Please try again.');
}
