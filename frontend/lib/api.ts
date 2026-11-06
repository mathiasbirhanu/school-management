export const API_BASE = process.env.NEXT_PUBLIC_API_BASE ?? "http://localhost:8080";

const ACCESS_KEY = "scholaris.access";
const REFRESH_KEY = "scholaris.refresh";
const USER_KEY = "scholaris.user";

export type SessionUser = {
  id: string;
  fullName: string;
  email: string;
  role: "Admin" | "Teacher" | "Student" | "Parent";
};

export function getAccessToken(): string | null {
  if (typeof window === "undefined") return null;
  return localStorage.getItem(ACCESS_KEY);
}

export function getSessionUser(): SessionUser | null {
  if (typeof window === "undefined") return null;
  try {
    const raw = localStorage.getItem(USER_KEY);
    return raw ? (JSON.parse(raw) as SessionUser) : null;
  } catch {
    return null;
  }
}

export function saveSession(access: string, refresh: string, user: SessionUser) {
  localStorage.setItem(ACCESS_KEY, access);
  localStorage.setItem(REFRESH_KEY, refresh);
  localStorage.setItem(USER_KEY, JSON.stringify(user));
}

export function clearSession() {
  localStorage.removeItem(ACCESS_KEY);
  localStorage.removeItem(REFRESH_KEY);
  localStorage.removeItem(USER_KEY);
}

export class ApiError extends Error {
  status: number;
  body: Record<string, unknown> | null;
  constructor(status: number, body: Record<string, unknown> | null) {
    super((body?.message as string) ?? `Request failed (${status})`);
    this.status = status;
    this.body = body;
  }
}

async function tryRefresh(): Promise<boolean> {
  if (typeof window === "undefined") return false;
  const refresh = localStorage.getItem(REFRESH_KEY);
  if (!refresh) return false;
  const res = await fetch(`${API_BASE}/api/auth/refresh`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ refreshToken: refresh })
  });
  if (!res.ok) {
    clearSession();
    return false;
  }
  const data = await res.json();
  saveSession(data.accessToken, data.refreshToken, data.user);
  return true;
}

export async function api<T = unknown>(
  path: string,
  options: { method?: string; body?: unknown; noAuth?: boolean } = {}
): Promise<T> {
  const doFetch = () =>
    fetch(`${API_BASE}${path}`, {
      method: options.method ?? "GET",
      headers: {
        "Content-Type": "application/json",
        ...(getAccessToken() && !options.noAuth
          ? { Authorization: `Bearer ${getAccessToken()}` }
          : {})
      },
      body: options.body === undefined ? undefined : JSON.stringify(options.body)
    });

  let res = await doFetch();
  if (res.status === 401 && getAccessToken() && (await tryRefresh())) res = await doFetch();
  if (!res.ok) throw new ApiError(res.status, await res.json().catch(() => null));
  if (res.status === 204) return undefined as T;
  return (await res.json()) as T;
}

/** Downloads an authenticated endpoint (e.g. CSV reports) as a file. */
export async function downloadFile(path: string, filename: string) {
  const res = await fetch(`${API_BASE}${path}`, {
    headers: getAccessToken() ? { Authorization: `Bearer ${getAccessToken()}` } : {}
  });
  if (!res.ok) throw new ApiError(res.status, null);
  const blob = await res.blob();
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}
