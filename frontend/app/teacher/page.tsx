"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { api, ApiError, downloadFile } from "@/lib/api";
import { fmtETB } from "@/lib/format";
import { useAuth } from "@/lib/auth";

type RevenueRow = { teacherName: string; month: string; gross: number; commission: number; net: number; orders: number };
type TeacherCourse = { id: string; title: string; studentCount: number; lessonCount: number };
type Thread = {
  id: string;
  courseId: string;
  question: string;
  status: "Open" | "Answered";
  createdAt: string;
  answer: string | null;
};

export default function TeacherPage() {
  const { user, ready } = useAuth();
  const router = useRouter();
  const [year, setYear] = useState(new Date().getFullYear());
  const [rows, setRows] = useState<RevenueRow[] | null>(null);
  const [courses, setCourses] = useState<TeacherCourse[]>([]);
  const [activeCourse, setActiveCourse] = useState<string>("");
  const [threads, setThreads] = useState<Thread[]>([]);
  const [drafts, setDrafts] = useState<Record<string, string>>({});

  useEffect(() => {
    if (ready && !user) router.replace("/login?next=/teacher");
    if (ready && user && user.role !== "Teacher") router.replace("/");
  }, [ready, user, router]);

  useEffect(() => {
    if (user?.role !== "Teacher") return;
    api<RevenueRow[]>(`/api/teacher/revenue?year=${year}`).then(setRows).catch(() => setRows([]));
    api<TeacherCourse[]>("/api/teacher/courses")
      .then((c) => {
        setCourses(c);
        if (c[0]) setActiveCourse(c[0].id);
      })
      .catch(() => setCourses([]));
  }, [user, year]);

  const loadThreads = useCallback(async (courseId: string) => {
    if (!courseId) return;
    try {
      setThreads(await api<Thread[]>(`/api/qa/threads?courseId=${courseId}`));
    } catch {
      setThreads([]);
    }
  }, []);

  useEffect(() => {
    let alive = true;
    (async () => {
      if (!activeCourse) return;
      try {
        const list = await api<Thread[]>(`/api/qa/threads?courseId=${activeCourse}`);
        if (alive) setThreads(list);
      } catch {
        if (alive) setThreads([]);
      }
    })();
    return () => {
      alive = false;
    };
  }, [activeCourse]);

  const answer = async (threadId: string) => {
    const text = (drafts[threadId] ?? "").trim();
    if (!text) return;
    try {
      await api(`/api/qa/threads/${threadId}/answer`, { method: "POST", body: { answer: text } });
      setDrafts((d) => ({ ...d, [threadId]: "" }));
      await loadThreads(activeCourse);
    } catch (e) {
      alert(e instanceof ApiError ? e.message : "Could not post the answer.");
    }
  };

  if (!user || user.role !== "Teacher")
    return <div className="py-20 text-center text-slate-500">Loading your desk…</div>;

  const totals = (rows ?? []).reduce(
    (a, r) => ({ gross: a.gross + r.gross, commission: a.commission + r.commission, net: a.net + r.net, orders: a.orders + r.orders }),
    { gross: 0, commission: 0, net: 0, orders: 0 }
  );

  return (
    <div className="mx-auto max-w-6xl px-4 py-10">
      <h1 className="text-3xl font-extrabold text-ink">Teacher desk</h1>
      <p className="mt-1 text-sm text-slate-500">Welcome, {user.fullName} — your income and your students&apos; questions.</p>

      {/* income */}
      <div className="mt-8 grid gap-4 sm:grid-cols-3">
        {[
          ["Gross income", totals.gross, "text-brand"],
          ["Platform service commission", totals.commission, "text-gold-dark"],
          ["Your net income", totals.net, "text-emerald-600"]
        ].map(([label, value, color]) => (
          <div key={label as string} className="rounded-2xl bg-white p-5 shadow-sm ring-1 ring-black/5">
            <p className="text-xs font-bold uppercase tracking-wide text-slate-400">{label as string}</p>
            <p className={`mt-1 text-2xl font-extrabold ${color as string}`}>{fmtETB(value as number)}</p>
          </div>
        ))}
      </div>

      <div className="mt-6 flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-2">
          <label className="text-sm font-bold text-slate-500">Year</label>
          <input
            type="number"
            value={year}
            onChange={(e) => setYear(Number(e.target.value))}
            className="w-24 rounded-xl border border-black/10 px-3 py-1.5 text-sm font-bold text-brand focus:border-brand focus:outline-none"
          />
        </div>
        <button
          onClick={() => downloadFile(`/api/teacher/revenue/csv?year=${year}`, `scholaris-teacher-income-${year}.csv`)}
          className="rounded-full bg-gold px-4 py-2 text-sm font-bold text-white shadow hover:bg-gold-dark"
        >
          ⬇ Download my income CSV
        </button>
      </div>

      <div className="mt-4 overflow-x-auto rounded-2xl bg-white shadow-sm ring-1 ring-black/5">
        <table className="w-full min-w-[640px] text-left text-sm">
          <thead>
            <tr className="border-b border-black/5 text-xs uppercase tracking-wide text-slate-400">
              <th className="px-4 py-3">Month</th>
              <th className="px-4 py-3">Gross</th>
              <th className="px-4 py-3">Service commission</th>
              <th className="px-4 py-3">Net income</th>
              <th className="px-4 py-3">Orders</th>
            </tr>
          </thead>
          <tbody>
            {rows?.map((r, i) => (
              <tr key={i} className="border-b border-black/5 last:border-0 hover:bg-brand-50/40">
                <td className="px-4 py-3 font-bold text-ink">{r.month}</td>
                <td className="px-4 py-3 font-semibold text-brand">{fmtETB(r.gross)}</td>
                <td className="px-4 py-3 text-gold-dark">{fmtETB(r.commission)}</td>
                <td className="px-4 py-3 text-emerald-700">{fmtETB(r.net)}</td>
                <td className="px-4 py-3 text-slate-500">{r.orders}</td>
              </tr>
            ))}
            {rows !== null && rows.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-10 text-center text-slate-400">
                  No sales recorded in {year} yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {/* Q&A inbox */}
      <h2 className="mt-12 text-xl font-extrabold text-ink">Student questions</h2>
      <div className="mt-3 flex flex-wrap gap-2">
        {courses.map((c) => (
          <button
            key={c.id}
            onClick={() => setActiveCourse(c.id)}
            className={`rounded-full px-4 py-1.5 text-xs font-bold transition ${
              activeCourse === c.id ? "bg-brand text-white shadow" : "bg-white text-slate-600 ring-1 ring-black/10 hover:bg-brand-50"
            }`}
          >
            {c.title}
          </button>
        ))}
        {courses.length === 0 && <p className="text-sm text-slate-400">No courses assigned yet.</p>}
      </div>

      <div className="mt-4 space-y-3">
        {threads.map((t) => (
          <div key={t.id} className="rounded-2xl bg-white p-4 shadow-sm ring-1 ring-black/5">
            <div className="flex items-start justify-between gap-3">
              <p className="font-semibold text-ink">{t.question}</p>
              <span
                className={`shrink-0 rounded-full px-2 py-0.5 text-[11px] font-bold ${
                  t.status === "Answered" ? "bg-emerald-100 text-emerald-700" : "bg-gold-50 text-gold-dark"
                }`}
              >
                {t.status}
              </span>
            </div>
            {t.answer ? (
              <p className="mt-2 rounded-xl bg-emerald-50 p-3 text-sm text-emerald-900">
                <b>Your reply: </b>
                {t.answer}
              </p>
            ) : (
              <div className="mt-2 flex gap-2">
                <input
                  value={drafts[t.id] ?? ""}
                  onChange={(e) => setDrafts((d) => ({ ...d, [t.id]: e.target.value }))}
                  placeholder="Write your answer…"
                  className="flex-1 rounded-xl border border-black/10 px-3 py-2 text-sm focus:border-brand focus:outline-none"
                />
                <button
                  onClick={() => answer(t.id)}
                  className="rounded-full bg-brand px-4 py-2 text-xs font-bold text-white hover:bg-brand-dark"
                >
                  Reply
                </button>
              </div>
            )}
          </div>
        ))}
        {threads.length === 0 && activeCourse && (
          <p className="text-sm text-slate-400">No questions for this course yet.</p>
        )}
      </div>
    </div>
  );
}
