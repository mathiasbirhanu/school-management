export function fmtETB(n: number | string): string {
  const value = typeof n === "string" ? Number(n) : n;
  return `ETB ${value.toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

export function fmtDate(iso: string | Date): string {
  return new Date(iso).toLocaleDateString("en-GB", { day: "numeric", month: "long", year: "numeric" });
}

export function fmtTime(seconds: number): string {
  if (!Number.isFinite(seconds)) return "0:00";
  const m = Math.floor(seconds / 60);
  const s = Math.floor(seconds % 60);
  return `${m}:${s.toString().padStart(2, "0")}`;
}

export const homeFor = (role?: string | null) =>
  role === "Admin" ? "/admin" : role === "Teacher" ? "/teacher" : "/student";
