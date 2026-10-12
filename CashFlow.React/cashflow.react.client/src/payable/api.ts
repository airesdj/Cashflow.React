import { ApiError } from "../api";

export class PayableError extends ApiError {
  readonly errors: Record<string, string[]>;
  readonly outcomeUnknown: boolean;
  constructor(
    status: number,
    message: string,
    errors: Record<string, string[]> = {},
    outcomeUnknown = false,
  ) {
    super(status, message);
    this.errors = errors;
    this.outcomeUnknown = outcomeUnknown;
  }
}

async function readError(response: Response): Promise<PayableError> {
  let body: {
    message?: string;
    title?: string;
    errors?: Record<string, string[]>;
    outcomeUnknown?: boolean;
  } = {};
  try {
    body = await response.json();
  } catch {
    /* Non-JSON proxy/server errors. */
  }
  const message =
    response.status === 401
      ? "Your session has expired. Please sign in again."
      : response.status === 403
        ? "You do not have permission to perform this action."
        : (body.message ??
          (response.status === 400
            ? "Check the entered details and try again."
            : "Unable to complete the request. Please try again."));
  return new PayableError(
    response.status,
    message,
    body.errors ?? {},
    body.outcomeUnknown ?? false,
  );
}

export async function loadPayable<T>(
  path: string,
  signal?: AbortSignal,
): Promise<T> {
  const response = await fetch(path, {
    credentials: "same-origin",
    cache: "no-store",
    signal,
  });
  if (!response.ok) throw await readError(response);
  return response.json();
}

export async function writePayable<T = { message: string }>(
  path: string,
  method: "POST" | "PUT" | "DELETE",
  body?: unknown,
): Promise<T> {
  // This token must match the identity that is signed in now.
  const { token } = await loadPayable<{ token: string }>(
    "/api/Login/Antiforgery",
  );
  let response: Response;
  try {
    response = await fetch(path, {
      method,
      credentials: "same-origin",
      headers: { "Content-Type": "application/json", "X-CSRF-TOKEN": token },
      body: body === undefined ? undefined : JSON.stringify(body),
    });
  } catch {
    // Never automatically retry a create/launch request whose outcome is unknown.
    throw new PayableError(
      0,
      "The connection was interrupted. The change may have been saved. Check the list before trying again.",
      {},
      true,
    );
  }
  if (!response.ok) throw await readError(response);
  try {
    return await response.json();
  } catch {
    throw new PayableError(
      0,
      "The server response could not be read. Check the list before trying again.",
      {},
      true,
    );
  }
}
