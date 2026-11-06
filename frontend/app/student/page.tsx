"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { api } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { fmtDate } from "@/lib/format";

type Enrollment = {
  courseId: string;
  slug: string;
  title: string;
  progressPercent: number;
  completedAt: string | null;
  certificateSerial: string | null;
};

type CertificateListItem = {
  serial: string;
  studentName: string;
  courseTitle: string;
  issuedAt: string;
  template: unknown;
};

export default function StudentPage() {
  const { user, ready } = useAuth();
  const router = useRouter();
  const [enrollments, setEnrollments] = useState<Enrollment[] | null>(null);
  const [certificates, setCertificates] = useState<CertificateListItem[] | null>(null);

  useEffect(() => {
    if (ready && !user) router.replace("/login?next=/student");
    if (!user) return;
    api<Enrollment[]>("/api/enrollments").then(setEnrollments).catch(() => setEnrollments([]));
    api<CertificateListItem[]>("/api/certificates/mine").then(setCertificates).catch(() => setCertificates([]));
  }, [user, ready, router]);

  if (!user) return <div className="py-20 text-center text-slate-500">Loading…</div>;

  return (
    <div className="mx-auto max-w-6xl px-4 py-10">
      <h1 className="text-3xl font-extrabold text-ink">My learning</h1>
      <p className="mt-1 text-sm text-slate-500">
        Welcome back, {user.fullName.split(" ")[0]} — keep going, your certificate awaits.
      </p>

      <div className="mt-8 grid gap-4 sm:grid-cols-2">
        {enrollments === null &&
          Array.from({ length: 2 }).map((_, i) => <div key={i} className="h-40 animate-pulse rounded-2xl bg-white" />)}
        {enrollments?.length === 0 && (
          <div className="rounded-2xl border border-dashed border-brand/30 bg-white p-8 sm:col-span-2">
            <p className="font-bold text-brand">No courses yet.</p>
            <Link href="/courses" className="mt-2 inline-block text-sm font-bold text-gold-dark hover:underline">
              Browse the catalog →
            </Link>
          </div>
        )}
        {enrollments?.map((e) => (
          <div key={e.courseId} className="rounded-2xl bg-white p-5 shadow-sm ring-1 ring-black/5">
            <div className="flex items-start justify-between gap-3">
              <h2 className="font-bold text-ink">{e.title}</h2>
              {e.certificateSerial && (
                <span className="shrink-0 rounded-full bg-gold-50 px-2 py-0.5 text-[11px] font-bold text-gold-dark">
                  🎓 Certified
                </span>
              )}
            </div>
            <div className="mt-3">
              <div className="flex justify-between text-xs font-semibold text-slate-500">
                <span>{e.progressPercent}% complete</span>
                {e.completedAt && <span>finished {fmtDate(e.completedAt)}</span>}
              </div>
              <div className="mt-1 h-2 overflow-hidden rounded-full bg-brand-100">
                <div
                  className="h-full rounded-full bg-gradient-to-r from-brand to-gold"
                  style={{ width: `${e.progressPercent}%` }}
                />
              </div>
            </div>
            <div className="mt-4 flex flex-wrap gap-2">
              <Link
                href={`/learn/${e.courseId}`}
                className="rounded-full bg-brand px-4 py-1.5 text-xs font-bold text-white hover:bg-brand-dark"
              >
                Continue →
              </Link>
              {e.certificateSerial && (
                <Link
                  href={`/certificate/${e.certificateSerial}`}
                  className="rounded-full bg-gold-50 px-4 py-1.5 text-xs font-bold text-gold-dark ring-1 ring-gold/30 hover:bg-gold/10"
                >
                  View certificate
                </Link>
              )}
            </div>
          </div>
        ))}
      </div>

      <h2 className="mt-12 text-xl font-extrabold text-ink">My certificates</h2>
      <div className="mt-3 space-y-2">
        {certificates?.length === 0 && (
          <p className="text-sm text-slate-500">
            Complete all lessons of a course and your certificate appears here automatically.
          </p>
        )}
        {certificates?.map((c) => (
          <Link
            key={c.serial}
            href={`/certificate/${c.serial}`}
            className="flex items-center justify-between rounded-2xl bg-white p-4 shadow-sm ring-1 ring-black/5 transition hover:-translate-y-0.5 hover:shadow-md"
          >
            <span>
              <span className="block font-bold text-ink">🎓 {c.courseTitle}</span>
              <span className="text-xs text-slate-400">Awarded {fmtDate(c.issuedAt)}</span>
            </span>
            <span className="font-mono text-xs text-slate-400">{c.serial}</span>
          </Link>
        ))}
      </div>
    </div>
  );
}
