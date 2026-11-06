"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import CertificateView, { type CertificateTemplateData } from "@/components/CertificateView";
import { api, ApiError, downloadFile } from "@/lib/api";
import { fmtETB } from "@/lib/format";
import { useAuth } from "@/lib/auth";

type AdminCourse = {
  id: string;
  slug: string;
  title: string;
  subject: string;
  level: string;
  summary: string;
  price: number;
  discountPercent: number;
  finalPrice: number;
  status: string;
  teacherId: string;
  teacherName: string;
  coverHue: number;
  lessonCount: number;
  studentCount: number;
};

type TeacherOption = { id: string; fullName: string; email: string };
type Settings = { commissionRatePercent: number; siteName: string; updatedAt: string };
type RevenueRow = { teacherName: string; month: string; gross: number; commission: number; net: number; orders: number };

const tabs = ["Courses", "Income rate", "Certificate", "Revenue"] as const;

export default function AdminPage() {
  const { user, ready } = useAuth();
  const router = useRouter();
  const [tab, setTab] = useState<(typeof tabs)[number]>("Courses");

  useEffect(() => {
    if (ready && user && user.role !== "Admin") router.replace("/");
    if (ready && !user) router.replace("/login?next=/admin");
  }, [ready, user, router]);

  if (!user || user.role !== "Admin")
    return <div className="py-20 text-center text-slate-500">Checking your admin access…</div>;

  return (
    <div className="mx-auto max-w-6xl px-4 py-10">
      <h1 className="text-3xl font-extrabold text-ink">Admin office</h1>
      <p className="mt-1 text-sm text-slate-500">
        You control the money side of Scholaris: courses, prices, discounts, the platform income rate,
        certificate wording — and you watch every birr in the revenue ledger.
      </p>

      <div className="mt-6 flex flex-wrap gap-2">
        {tabs.map((t) => (
          <button
            key={t}
            onClick={() => setTab(t)}
            className={`rounded-full px-4 py-2 text-sm font-bold transition ${
              tab === t ? "bg-brand text-white shadow" : "bg-white text-slate-600 ring-1 ring-black/10 hover:bg-brand-50"
            }`}
          >
            {t}
          </button>
        ))}
      </div>

      <div className="mt-6">
        {tab === "Courses" && <CoursesTab />}
        {tab === "Income rate" && <SettingsTab />}
        {tab === "Certificate" && <CertificateTab />}
        {tab === "Revenue" && <RevenueTab />}
      </div>
    </div>
  );
}

/* ─────────────────────────── Courses tab ─────────────────────────── */

function CoursesTab() {
  const [courses, setCourses] = useState<AdminCourse[] | null>(null);
  const [teachers, setTeachers] = useState<TeacherOption[]>([]);
  const [editing, setEditing] = useState<AdminCourse | "new" | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      setCourses(await api<AdminCourse[]>("/api/admin/courses"));
      setTeachers(await api<TeacherOption[]>("/api/admin/teachers"));
    } catch (e) {
      setMessage(e instanceof ApiError ? e.message : "Could not load courses.");
    }
  }, []);

  useEffect(() => {
    let alive = true;
    (async () => {
      try {
        const list = await api<AdminCourse[]>("/api/admin/courses");
        const teachers = await api<TeacherOption[]>("/api/admin/teachers");
        if (!alive) return;
        setCourses(list);
        setTeachers(teachers);
      } catch (e) {
        if (alive) setMessage(e instanceof ApiError ? e.message : "Could not load courses.");
      }
    })();
    return () => {
      alive = false;
    };
  }, []);

  const remove = async (c: AdminCourse) => {
    if (!confirm(`Delete "${c.title}"? This cannot be undone.`)) return;
    try {
      await api(`/api/admin/courses/${c.id}`, { method: "DELETE" });
      setMessage(`Deleted "${c.title}".`);
      await load();
    } catch (e) {
      setMessage(e instanceof ApiError ? e.message : "Delete failed.");
    }
  };

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <p className="text-sm text-slate-500">
          {courses?.length ?? 0} courses · prices, discounts and publishing are admin-only decisions.
        </p>
        <button
          onClick={() => setEditing("new")}
          className="rounded-full bg-gold px-4 py-2 text-sm font-bold text-white shadow hover:bg-gold-dark"
        >
          + New course
        </button>
      </div>

      {message && (
        <p className="mb-4 rounded-xl bg-brand-50 px-4 py-2 text-sm font-semibold text-brand">{message}</p>
      )}

      <div className="overflow-x-auto rounded-2xl bg-white shadow-sm ring-1 ring-black/5">
        <table className="w-full min-w-[760px] text-left text-sm">
          <thead>
            <tr className="border-b border-black/5 text-xs uppercase tracking-wide text-slate-400">
              <th className="px-4 py-3">Course</th>
              <th className="px-4 py-3">Teacher</th>
              <th className="px-4 py-3">Price</th>
              <th className="px-4 py-3">Discount</th>
              <th className="px-4 py-3">Status</th>
              <th className="px-4 py-3">Students</th>
              <th className="px-4 py-3"></th>
            </tr>
          </thead>
          <tbody>
            {courses?.map((c) => (
              <tr key={c.id} className="border-b border-black/5 last:border-0 hover:bg-brand-50/40">
                <td className="px-4 py-3">
                  <p className="font-bold text-ink">{c.title}</p>
                  <p className="text-xs text-slate-400">
                    {c.subject} · {c.level} · {c.lessonCount} lessons
                  </p>
                </td>
                <td className="px-4 py-3 text-slate-600">{c.teacherName}</td>
                <td className="px-4 py-3">
                  <span className="font-semibold text-brand">{fmtETB(c.finalPrice)}</span>
                  {c.discountPercent > 0 && (
                    <span className="block text-xs text-slate-400 line-through">{fmtETB(c.price)}</span>
                  )}
                </td>
                <td className="px-4 py-3">
                  {c.discountPercent > 0 ? (
                    <span className="rounded-full bg-gold px-2 py-0.5 text-xs font-bold text-white">
                      -{c.discountPercent}%
                    </span>
                  ) : (
                    <span className="text-slate-300">—</span>
                  )}
                </td>
                <td className="px-4 py-3">
                  <span
                    className={`rounded-full px-2 py-0.5 text-xs font-bold ${
                      c.status === "Published"
                        ? "bg-emerald-100 text-emerald-700"
                        : c.status === "Draft"
                          ? "bg-slate-100 text-slate-500"
                          : "bg-rose-100 text-rose-600"
                    }`}
                  >
                    {c.status}
                  </span>
                </td>
                <td className="px-4 py-3 text-slate-600">{c.studentCount}</td>
                <td className="px-4 py-3">
                  <div className="flex gap-2">
                    <button
                      onClick={() => setEditing(c)}
                      className="rounded-full bg-brand-50 px-3 py-1 text-xs font-bold text-brand hover:bg-brand-100"
                    >
                      Edit
                    </button>
                    <button
                      onClick={() => remove(c)}
                      className="rounded-full bg-rose-50 px-3 py-1 text-xs font-bold text-rose-600 hover:bg-rose-100"
                    >
                      Delete
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {editing && (
        <CourseForm
          course={editing === "new" ? null : editing}
          teachers={teachers}
          onClose={() => setEditing(null)}
          onSaved={async (msg) => {
            setEditing(null);
            setMessage(msg);
            await load();
          }}
        />
      )}
    </div>
  );
}

function CourseForm({
  course,
  teachers,
  onClose,
  onSaved
}: {
  course: AdminCourse | null;
  teachers: TeacherOption[];
  onClose: () => void;
  onSaved: (message: string) => void;
}) {
  const [form, setForm] = useState({
    title: course?.title ?? "",
    subject: course?.subject ?? "",
    level: course?.level ?? "Grade 8",
    summary: course?.summary ?? "",
    price: course?.price ?? 400,
    discountPercent: course?.discountPercent ?? 0,
    status: course?.status ?? "Published",
    teacherId: course?.teacherId ?? teachers[0]?.id ?? "",
    coverHue: course?.coverHue ?? 258
  });
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const set = (k: string, v: string | number) => setForm((f) => ({ ...f, [k]: v }));

  const save = async () => {
    setBusy(true);
    setError(null);
    try {
      if (course) {
        await api(`/api/admin/courses/${course.id}`, { method: "PUT", body: form });
        onSaved(`Updated "${form.title}".`);
      } else {
        await api("/api/admin/courses", { method: "POST", body: form });
        onSaved(`Created "${form.title}". Add its curriculum next via the API/seed (v1 note in README).`);
      }
    } catch (e) {
      setError(e instanceof ApiError ? e.message : "Save failed.");
      setBusy(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
      <div className="nice-scroll max-h-[90vh] w-full max-w-lg overflow-y-auto rounded-3xl bg-white p-6 shadow-2xl">
        <h3 className="text-lg font-extrabold text-brand">{course ? "Edit course" : "New course"}</h3>

        <div className="mt-4 space-y-3 text-sm">
          {[
            ["title", "Title"],
            ["subject", "Subject"],
            ["level", "Level (e.g. Grade 8, College Year 1)"]
          ].map(([key, label]) => (
            <div key={key}>
              <label className="text-xs font-bold uppercase tracking-wide text-slate-400">{label}</label>
              <input
                value={String(form[key as keyof typeof form])}
                onChange={(e) => set(key, e.target.value)}
                className="mt-1 w-full rounded-xl border border-black/10 px-3 py-2 focus:border-brand focus:outline-none"
              />
            </div>
          ))}
          <div>
            <label className="text-xs font-bold uppercase tracking-wide text-slate-400">Summary</label>
            <textarea
              rows={2}
              value={form.summary}
              onChange={(e) => set("summary", e.target.value)}
              className="mt-1 w-full rounded-xl border border-black/10 px-3 py-2 focus:border-brand focus:outline-none"
            />
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="text-xs font-bold uppercase tracking-wide text-slate-400">Price (ETB)</label>
              <input
                type="number"
                min={0}
                value={form.price}
                onChange={(e) => set("price", Number(e.target.value))}
                className="mt-1 w-full rounded-xl border border-black/10 px-3 py-2 focus:border-brand focus:outline-none"
              />
            </div>
            <div>
              <label className="text-xs font-bold uppercase tracking-wide text-slate-400">Discount %</label>
              <input
                type="number"
                min={0}
                max={100}
                value={form.discountPercent}
                onChange={(e) => set("discountPercent", Number(e.target.value))}
                className="mt-1 w-full rounded-xl border border-black/10 px-3 py-2 focus:border-brand focus:outline-none"
              />
            </div>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="text-xs font-bold uppercase tracking-wide text-slate-400">Status</label>
              <select
                value={form.status}
                onChange={(e) => set("status", e.target.value)}
                className="mt-1 w-full rounded-xl border border-black/10 px-3 py-2 focus:border-brand focus:outline-none"
              >
                {["Draft", "Published", "Archived"].map((s) => (
                  <option key={s}>{s}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="text-xs font-bold uppercase tracking-wide text-slate-400">Cover color</label>
              <input
                type="number"
                min={0}
                max={360}
                value={form.coverHue}
                onChange={(e) => set("coverHue", Number(e.target.value))}
                className="mt-1 w-full rounded-xl border border-black/10 px-3 py-2 focus:border-brand focus:outline-none"
              />
            </div>
          </div>
          <div>
            <label className="text-xs font-bold uppercase tracking-wide text-slate-400">Teacher</label>
            <select
              value={form.teacherId}
              onChange={(e) => set("teacherId", e.target.value)}
              className="mt-1 w-full rounded-xl border border-black/10 px-3 py-2 focus:border-brand focus:outline-none"
            >
              <option value="">Select a teacher…</option>
              {teachers.map((t) => (
                <option key={t.id} value={t.id}>
                  {t.fullName} ({t.email})
                </option>
              ))}
            </select>
          </div>
        </div>

        {error && <p className="mt-3 rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-600">{error}</p>}

        <div className="mt-5 flex justify-end gap-3">
          <button onClick={onClose} className="rounded-full px-4 py-2 text-sm font-bold text-slate-500 hover:bg-slate-100">
            Cancel
          </button>
          <button
            onClick={save}
            disabled={busy || !form.teacherId}
            className="rounded-full bg-brand px-5 py-2 text-sm font-bold text-white shadow hover:bg-brand-dark disabled:opacity-50"
          >
            {busy ? "Saving…" : course ? "Save changes" : "Create course"}
          </button>
        </div>
      </div>
    </div>
  );
}

/* ─────────────────────────── Income rate tab ─────────────────────────── */

function SettingsTab() {
  const [settings, setSettings] = useState<Settings | null>(null);
  const [rate, setRate] = useState(22);
  const [siteName, setSiteName] = useState("Scholaris");
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    api<Settings>("/api/admin/settings")
      .then((s) => {
        setSettings(s);
        setRate(s.commissionRatePercent);
        setSiteName(s.siteName);
      })
      .catch(() => setMessage("Could not load settings."));
  }, []);

  const save = async () => {
    setBusy(true);
    setMessage(null);
    try {
      const s = await api<Settings>("/api/admin/settings", {
        method: "PUT",
        body: { commissionRatePercent: rate, siteName }
      });
      setSettings(s);
      setMessage(`Saved. New sales now carry a ${s.commissionRatePercent}% service commission (past orders keep their snapshot).`);
    } catch (e) {
      setMessage(e instanceof ApiError ? e.message : "Save failed.");
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="max-w-xl rounded-2xl bg-white p-6 shadow-sm ring-1 ring-black/5">
      <h2 className="font-extrabold text-brand">Platform income rate</h2>
      <p className="mt-1 text-sm text-slate-500">
        The service commission Scholaris keeps from every sale. Teachers receive the rest. Each order
        stores the rate that was active at payment time, so changing this never rewrites history.
      </p>

      <div className="mt-5 grid grid-cols-[130px_1fr] items-center gap-4">
        <div>
          <label className="text-xs font-bold uppercase tracking-wide text-slate-400">Rate %</label>
          <input
            type="number"
            min={0}
            max={90}
            step={0.5}
            value={rate}
            onChange={(e) => setRate(Number(e.target.value))}
            className="mt-1 w-full rounded-xl border border-black/10 px-3 py-2 text-lg font-extrabold text-brand focus:border-brand focus:outline-none"
          />
        </div>
        <div className="rounded-xl bg-brand-50 p-4 text-sm text-brand">
          Example: a <b>ETB 1,000.00</b> sale → platform service income{" "}
          <b>ETB {(rate * 10).toFixed(2)}</b>, teacher income <b>ETB {(1000 - rate * 10).toFixed(2)}</b>.
        </div>
      </div>

      <div className="mt-4">
        <label className="text-xs font-bold uppercase tracking-wide text-slate-400">Site name</label>
        <input
          value={siteName}
          onChange={(e) => setSiteName(e.target.value)}
          className="mt-1 w-full rounded-xl border border-black/10 px-3 py-2 text-sm focus:border-brand focus:outline-none"
        />
      </div>

      {message && <p className="mt-4 rounded-xl bg-emerald-50 px-4 py-2 text-sm text-emerald-700">{message}</p>}

      <button
        onClick={save}
        disabled={busy}
        className="mt-5 rounded-full bg-brand px-6 py-2.5 text-sm font-bold text-white shadow hover:bg-brand-dark disabled:opacity-50"
      >
        {busy ? "Saving…" : "Save income rate"}
      </button>
      {settings && (
        <p className="mt-3 text-xs text-slate-400">Last updated {new Date(settings.updatedAt).toLocaleString()}</p>
      )}
    </div>
  );
}

/* ─────────────────────────── Certificate tab ─────────────────────────── */

function CertificateTab() {
  const [template, setTemplate] = useState<CertificateTemplateData | null>(null);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    api<CertificateTemplateData>("/api/admin/certificate-template")
      .then(setTemplate)
      .catch(() => setMessage("Could not load the certificate template."));
  }, []);

  const save = async () => {
    if (!template) return;
    setBusy(true);
    setMessage(null);
    try {
      setTemplate(
        await api<CertificateTemplateData>("/api/admin/certificate-template", {
          method: "PUT",
          body: template
        })
      );
      setMessage("Certificate template saved — new certificates render with this wording.");
    } catch (e) {
      setMessage(e instanceof ApiError ? e.message : "Save failed.");
    } finally {
      setBusy(false);
    }
  };

  if (!template) return <div className="h-80 animate-pulse rounded-2xl bg-white" />;

  const field = (key: keyof CertificateTemplateData, label: string, rows = 0) => (
    <div>
      <label className="text-xs font-bold uppercase tracking-wide text-slate-400">{label}</label>
      {rows > 0 ? (
        <textarea
          rows={rows}
          value={template[key]}
          onChange={(e) => setTemplate({ ...template, [key]: e.target.value })}
          className="mt-1 w-full rounded-xl border border-black/10 px-3 py-2 text-sm focus:border-brand focus:outline-none"
        />
      ) : (
        <input
          value={template[key]}
          onChange={(e) => setTemplate({ ...template, [key]: e.target.value })}
          className="mt-1 w-full rounded-xl border border-black/10 px-3 py-2 text-sm focus:border-brand focus:outline-none"
        />
      )}
    </div>
  );

  return (
    <div className="grid gap-6 lg:grid-cols-[420px_1fr]">
      <div className="space-y-3 rounded-2xl bg-white p-6 shadow-sm ring-1 ring-black/5">
        <h2 className="font-extrabold text-brand">Certificate template</h2>
        <p className="text-xs text-slate-400">
          Placeholders: <code>{"{{StudentName}}"}</code> <code>{"{{CourseTitle}}"}</code>{" "}
          <code>{"{{Date}}"}</code> <code>{"{{Serial}}"}</code> — <code>{"{{StudentName}}"}</code> is required.
        </p>
        {field("title", "Title")}
        {field("subtitle", "Subtitle")}
        {field("bodyTemplate", "Body (one line per paragraph)", 7)}
        {field("footerNote", "Footer note")}
        <div className="grid grid-cols-2 gap-3">
          {field("signatoryName", "Signatory name")}
          {field("signatoryTitle", "Signatory title")}
        </div>
        {field("accentHex", "Accent color (hex)")}
        {message && <p className="rounded-xl bg-emerald-50 px-4 py-2 text-sm text-emerald-700">{message}</p>}
        <button
          onClick={save}
          disabled={busy}
          className="w-full rounded-full bg-brand py-2.5 text-sm font-bold text-white shadow hover:bg-brand-dark disabled:opacity-50"
        >
          {busy ? "Saving…" : "Save template"}
        </button>
      </div>

      <div>
        <p className="mb-2 text-xs font-bold uppercase tracking-wide text-slate-400">Live preview</p>
        <CertificateView
          serial="SCH-2026-PREVIEW"
          studentName="Abdi Nagasa"
          courseTitle="Introduction to Python Programming"
          issuedAt={new Date().toISOString()}
          template={template}
        />
      </div>
    </div>
  );
}

/* ─────────────────────────── Revenue tab ─────────────────────────── */

function RevenueTab() {
  const [year, setYear] = useState(new Date().getFullYear());
  const [state, setState] = useState<{ year: number; rows: RevenueRow[] } | null>(null);
  const rows = state?.year === year ? state.rows : null;

  useEffect(() => {
    let alive = true;
    api<RevenueRow[]>(`/api/admin/revenue?year=${year}`)
      .then((rows) => alive && setState({ year, rows }))
      .catch(() => alive && setState({ year, rows: [] }));
    return () => {
      alive = false;
    };
  }, [year]);

  const totals = (rows ?? []).reduce(
    (a, r) => ({ gross: a.gross + r.gross, commission: a.commission + r.commission, net: a.net + r.net, orders: a.orders + r.orders }),
    { gross: 0, commission: 0, net: 0, orders: 0 }
  );

  return (
    <div>
      <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
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
          onClick={() => downloadFile(`/api/admin/revenue/csv?year=${year}`, `scholaris-revenue-${year}.csv`)}
          className="rounded-full bg-gold px-4 py-2 text-sm font-bold text-white shadow hover:bg-gold-dark"
        >
          ⬇ Download CSV
        </button>
      </div>

      <div className="overflow-x-auto rounded-2xl bg-white shadow-sm ring-1 ring-black/5">
        <table className="w-full min-w-[720px] text-left text-sm">
          <thead>
            <tr className="border-b border-black/5 text-xs uppercase tracking-wide text-slate-400">
              <th className="px-4 py-3">Teacher</th>
              <th className="px-4 py-3">Month</th>
              <th className="px-4 py-3">Gross income</th>
              <th className="px-4 py-3">Service commission</th>
              <th className="px-4 py-3">Teacher net</th>
              <th className="px-4 py-3">Orders</th>
            </tr>
          </thead>
          <tbody>
            {rows?.map((r, i) => (
              <tr key={i} className="border-b border-black/5 last:border-0 hover:bg-brand-50/40">
                <td className="px-4 py-3 font-bold text-ink">{r.teacherName}</td>
                <td className="px-4 py-3 text-slate-600">{r.month}</td>
                <td className="px-4 py-3 font-semibold text-brand">{fmtETB(r.gross)}</td>
                <td className="px-4 py-3 text-gold-dark">{fmtETB(r.commission)}</td>
                <td className="px-4 py-3 text-emerald-700">{fmtETB(r.net)}</td>
                <td className="px-4 py-3 text-slate-500">{r.orders}</td>
              </tr>
            ))}
            {rows !== null && rows.length === 0 && (
              <tr>
                <td colSpan={6} className="px-4 py-10 text-center text-slate-400">
                  No paid orders in {year} yet.
                </td>
              </tr>
            )}
          </tbody>
          {rows && rows.length > 0 && (
            <tfoot>
              <tr className="border-t-2 border-brand/20 bg-brand-50/50 font-extrabold text-brand">
                <td className="px-4 py-3" colSpan={2}>
                  Total {year}
                </td>
                <td className="px-4 py-3">{fmtETB(totals.gross)}</td>
                <td className="px-4 py-3">{fmtETB(totals.commission)}</td>
                <td className="px-4 py-3">{fmtETB(totals.net)}</td>
                <td className="px-4 py-3">{totals.orders}</td>
              </tr>
            </tfoot>
          )}
        </table>
      </div>
      <p className="mt-3 text-xs text-slate-400">
        Gross = what students paid after admin discounts · Service commission = platform income at the
        rate snapshot per order · Net = teacher income.
      </p>
    </div>
  );
}
