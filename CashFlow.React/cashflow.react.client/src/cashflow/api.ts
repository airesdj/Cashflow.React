import { ApiError } from "../api";

export class CashflowError extends ApiError {
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

async function readError(response: Response): Promise<CashflowError> {
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
  return new CashflowError(
    response.status,
    message,
    body.errors ?? {},
    body.outcomeUnknown ?? false,
  );
}

export async function loadCashflow<T>(
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
