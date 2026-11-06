"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { useAuth } from "@/lib/auth";

export default function RegisterPage() {
  const { register } = useAuth();
  const router = useRouter();
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      await register(fullName, email, password);
      router.push("/courses"); // new students go pick a course
    } catch (err) {
      setError(err instanceof Error ? err.message : "Registration failed.");
      setBusy(false);
    }
  };

  return (
    <div className="mx-auto max-w-md px-4 py-16">
      <div className="rounded-3xl bg-white p-8 shadow-xl ring-1 ring-black/5">
        <h1 className="text-2xl font-extrabold text-ink">Create your student account</h1>
        <p className="mt-1 text-sm text-slate-500">
          Free to join — you only pay for the courses you enroll in.
        </p>

        <form onSubmit={submit} className="mt-6 space-y-4">
          <div>
            <label className="text-xs font-bold uppercase tracking-wide text-slate-400" htmlFor="name">
              Full name
            </label>
            <input
              id="name"
              required
              value={fullName}
              onChange={(e) => setFullName(e.target.value)}
              className="mt-1 w-full rounded-xl border border-black/10 px-4 py-2.5 text-sm focus:border-brand focus:outline-none"
              placeholder="Abi Alemayehu"
            />
          </div>
          <div>
            <label className="text-xs font-bold uppercase tracking-wide text-slate-400" htmlFor="email">
              Email
            </label>
            <input
              id="email"
              type="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="mt-1 w-full rounded-xl border border-black/10 px-4 py-2.5 text-sm focus:border-brand focus:outline-none"
              placeholder="you@example.com"
            />
          </div>
          <div>
            <label className="text-xs font-bold uppercase tracking-wide text-slate-400" htmlFor="password">
              Password
            </label>
            <input
              id="password"
              type="password"
              required
              minLength={6}
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="mt-1 w-full rounded-xl border border-black/10 px-4 py-2.5 text-sm focus:border-brand focus:outline-none"
              placeholder="At least 6 characters"
            />
          </div>

          {error && <p className="rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-600">{error}</p>}

          <button
            type="submit"
            disabled={busy}
            className="w-full rounded-full bg-gold py-3 text-sm font-bold text-white shadow-lg shadow-gold/30 transition hover:bg-gold-dark disabled:opacity-50"
          >
            {busy ? "Creating account…" : "Create account"}
          </button>
        </form>

        <p className="mt-4 text-center text-sm text-slate-500">
          Already have an account?{" "}
          <Link href="/login" className="font-bold text-brand hover:underline">
            Sign in
          </Link>
        </p>
      </div>
    </div>
  );
}
