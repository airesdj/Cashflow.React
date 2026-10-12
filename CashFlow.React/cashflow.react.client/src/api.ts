export class ApiError extends Error {
  readonly status: number;
  constructor(status: number, message: string) { super(message); this.status = status; }
}

export async function getJson<T>(path: string, signal?: AbortSignal): Promise<T> {
  const response = await fetch(path, { credentials: 'same-origin', cache: 'no-store', signal });
  if (response.status === 401) throw new ApiError(401, 'Your session has expired. Please sign in again.');
  if (!response.ok) {
    let message = response.status >= 500 ? 'Unable to load the dashboard. Please try again.' : 'Unable to complete the request.';
    try {
      const body: { message?: string } = await response.json();
      if (body.message) message = body.message;
    } catch { /* The server may return a non-JSON error response. */ }
    throw new ApiError(response.status, message);
  }
  return response.json();
}
