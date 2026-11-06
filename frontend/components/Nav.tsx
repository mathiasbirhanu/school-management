"use client";

import Link from "next/link";
import { useState } from "react";
import Logo from "./Logo";
import { useAuth } from "@/lib/auth";
import { homeFor } from "@/lib/format";

export default function Nav() {
  const { user, ready, logout } = useAuth();
  const [open, setOpen] = useState(false);

  const dashboardLabel =
    user?.role === "Admin" ? "Admin office" : user?.role === "Teacher" ? "Teacher desk" : "My learning";

  return (
    <header className="sticky top-0 z-40 border-b border-black/5 bg-white/85 backdrop-blur print-hide">
      <div className="mx-auto flex max-w-6xl items-center justify-between gap-4 px-4 py-3">
        <Link href="/" aria-label="Scholaris home">
          <Logo />
        </Link>

        <nav className="hidden items-center gap-6 text-sm font-semibold text-slate-600 md:flex">
          <Link className="hover:text-brand" href="/courses">
            Courses
          </Link>
          {user && (
            <Link className="hover:text-brand" href={homeFor(user.role)}>
              {dashboardLabel}
            </Link>
          )}
          {user?.role === "Student" && (
            <Link className="hover:text-brand" href="/student">
              My certificates
            </Link>
          )}
        </nav>

        <div className="hidden items-center gap-3 md:flex">
          {ready && user ? (
            <>
              <span className="max-w-[160px] truncate text-sm font-semibold text-slate-700">
                {user.fullName}
              </span>
              <button
                onClick={logout}
                className="rounded-full border border-brand/20 px-4 py-1.5 text-sm font-semibold text-brand hover:bg-brand-50"
              >
                Sign out
              </button>
            </>
          ) : (
            <>
              <Link
                href="/login"
                className="rounded-full px-4 py-1.5 text-sm font-semibold text-brand hover:bg-brand-50"
              >
                Sign in
              </Link>
              <Link
                href="/register"
                className="rounded-full bg-gold px-4 py-1.5 text-sm font-bold text-white shadow hover:bg-gold-dark"
              >
                Get started
              </Link>
            </>
          )}
        </div>

        <button
          className="rounded-lg p-2 text-brand md:hidden"
          onClick={() => setOpen(!open)}
          aria-label="Toggle menu"
        >
          <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.4">
            {open ? <path d="M5 5l14 14M19 5L5 19" /> : <path d="M4 7h16M4 12h16M4 17h16" />}
          </svg>
        </button>
      </div>

      {open && (
        <div className="border-t border-black/5 bg-white px-4 py-3 md:hidden">
          <div className="flex flex-col gap-3 text-sm font-semibold text-slate-700">
            <Link href="/courses" onClick={() => setOpen(false)}>
              Courses
            </Link>
            {user && (
              <Link href={homeFor(user.role)} onClick={() => setOpen(false)}>
                {dashboardLabel}
              </Link>
            )}
            {user?.role === "Student" && (
              <Link href="/student" onClick={() => setOpen(false)}>
                My certificates
              </Link>
            )}
            {user ? (
              <button className="text-left text-brand" onClick={logout}>
                Sign out
              </button>
            ) : (
              <div className="flex gap-3">
                <Link className="text-brand" href="/login">
                  Sign in
                </Link>
                <Link className="text-gold" href="/register">
                  Get started
                </Link>
              </div>
            )}
          </div>
        </div>
      )}
    </header>
  );
}
