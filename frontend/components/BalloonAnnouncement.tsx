"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { api } from "@/lib/api";
import { fmtETB } from "@/lib/format";
import type { CourseListItem } from "./CourseCard";

/**
 * Floating discount balloon: appears only when the admin has discounted courses.
 * Clicking it opens the list of discounted courses (price + -% tag).
 */
export default function BalloonAnnouncement() {
  const [courses, setCourses] = useState<CourseListItem[]>([]);
  const [open, setOpen] = useState(false);

  useEffect(() => {
    api<CourseListItem[]>("/api/courses/discounted", { noAuth: true })
      .then(setCourses)
      .catch(() => setCourses([]));
  }, []);

  if (courses.length === 0) return null;

  return (
    <div className="fixed bottom-6 right-5 z-50 print-hide">
      {open && (
        <>
          <div className="fixed inset-0 z-40" onClick={() => setOpen(false)} aria-hidden />
          <div className="absolute bottom-24 right-0 z-50 w-80 animate-fadeUp rounded-2xl bg-white p-4 shadow-2xl ring-1 ring-black/10">
            <div className="mb-3 flex items-center justify-between">
              <h3 className="flex items-center gap-2 font-extrabold text-brand">
                <span aria-hidden>🎉</span> Discounted courses
              </h3>
              <button
                onClick={() => setOpen(false)}
                className="rounded-full px-2 text-slate-400 hover:bg-slate-100"
                aria-label="Close"
              >
                ✕
              </button>
            </div>
            <ul className="nice-scroll max-h-72 space-y-2 overflow-y-auto pr-1">
              {courses.map((c) => (
                <li key={c.id}>
                  <Link
                    href={`/courses/${c.slug}`}
                    onClick={() => setOpen(false)}
                    className="flex items-center gap-3 rounded-xl p-2 transition hover:bg-brand-50"
                  >
                    <span
                      className="h-11 w-14 shrink-0 rounded-lg"
                      style={{
                        background: `linear-gradient(135deg, hsl(${c.coverHue}, 52%, 50%), hsl(${(c.coverHue + 42) % 360}, 58%, 36%))`
                      }}
                      aria-hidden
                    />
                    <span className="min-w-0 flex-1">
                      <span className="block truncate text-sm font-bold text-ink">{c.title}</span>
                      <span className="text-xs text-slate-500">{c.teacherName}</span>
                    </span>
                    <span className="text-right">
                      <span className="block text-sm font-extrabold text-brand">
                        {fmtETB(c.finalPrice)}
                      </span>
                      <span className="text-[11px] text-slate-400 line-through">
                        {fmtETB(c.price)}
                      </span>
                    </span>
                    <span className="rounded-full bg-gold px-1.5 py-0.5 text-[11px] font-extrabold text-white">
                      -{c.discountPercent}%
                    </span>
                  </Link>
                </li>
              ))}
            </ul>
            <p className="mt-3 border-t border-black/5 pt-2 text-[11px] text-slate-400">
              Prices and discounts are set by the Scholaris admin office.
            </p>
          </div>
        </>
      )}

      <button
        onClick={() => setOpen(!open)}
        className="relative block animate-floaty"
        aria-label={`${courses.length} discounted courses — open announcement`}
      >
        <svg width="64" height="86" viewBox="0 0 64 86" aria-hidden>
          <ellipse cx="26" cy="34" rx="17" ry="22" fill="#F0A731" />
          <ellipse cx="19" cy="26" rx="5" ry="8" fill="#FFFFFF" opacity="0.4" transform="rotate(-20 19 26)" />
          <path d="M26 56l-5 5 5 5-5 5" fill="none" stroke="#D18B1A" strokeWidth="2" strokeLinecap="round" />
          <path d="M21 56h10l-3-6h-4z" fill="#D18B1A" />
          <ellipse cx="40" cy="22" rx="12" ry="15" fill="#3F3D8F" />
          <ellipse cx="36" cy="16" rx="3.4" ry="5" fill="#FFFFFF" opacity="0.35" transform="rotate(-20 36 16)" />
        </svg>
        <span className="absolute -right-1 -top-1 flex h-6 min-w-6 items-center justify-center rounded-full bg-brand px-1 text-xs font-extrabold text-white ring-2 ring-white">
          {courses.length}
        </span>
      </button>
    </div>
  );
}
