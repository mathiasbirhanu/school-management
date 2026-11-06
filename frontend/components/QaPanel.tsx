"use client";

import { useCallback, useEffect, useState } from "react";
import { api, ApiError } from "@/lib/api";
import { fmtDate } from "@/lib/format";

type Thread = {
  id: string;
  courseId: string;
  courseTitle: string;
  lessonId: string | null;
  question: string;
  status: "Open" | "Answered";
  createdAt: string;
  answer: string | null;
  answeredAt: string | null;
};

/** Student ↔ teacher Q&A. (The AI answers instantly and separately in its own panels.) */
export default function QaPanel({
  courseId,
  lessonId
}: {
  courseId: string;
  lessonId?: string | null;
}) {
  const [threads, setThreads] = useState<Thread[]>([]);
  const [question, setQuestion] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      const all = await api<Thread[]>("/api/qa/threads");
      setThreads(all.filter((t) => t.courseId === courseId));
    } catch {
      setThreads([]);
    }
  }, [courseId]);

  useEffect(() => {
    void load();
  }, [load]);

  const ask = async () => {
    if (!question.trim()) return;
    setBusy(true);
    setError(null);
    try {
      await api<Thread>("/api/qa/threads", {
        method: "POST",
        body: { courseId, lessonId: lessonId ?? null, question }
      });
      setQuestion("");
      await load();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : "Could not send your question.");
    } finally {
      setBusy(false);
    }
  };

  return (
    <section className="rounded-2xl bg-white p-5 shadow-sm ring-1 ring-black/5">
      <h3 className="font-extrabold text-brand">Ask the teacher</h3>
      <p className="mt-1 text-[13px] text-slate-500">
        Need a human answer? Send your question to the course teacher. (For instant help, use the AI
        summary and the AI practice test above.)
      </p>
      <textarea
        rows={2}
        value={question}
        onChange={(e) => setQuestion(e.target.value)}
        placeholder="e.g. I don't understand step 3 of the worked example…"
        className="mt-3 w-full resize-y rounded-lg border border-black/10 p-2.5 text-sm focus:border-brand focus:outline-none"
      />
      {error && <p className="mt-2 rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-600">{error}</p>}
      <button
        onClick={ask}
        disabled={busy || !question.trim()}
        className="mt-2 rounded-full bg-brand px-5 py-2 text-sm font-bold text-white shadow transition hover:bg-brand-dark disabled:opacity-50"
      >
        {busy ? "Sending…" : "Send question"}
      </button>

      <div className="mt-4 space-y-3">
        {threads.map((t) => (
          <div key={t.id} className="rounded-xl border border-black/5 bg-[#FAFAFE] p-3">
            <div className="flex items-start justify-between gap-2">
              <p className="text-sm font-semibold text-ink">{t.question}</p>
              <span
                className={`shrink-0 rounded-full px-2 py-0.5 text-[11px] font-bold ${
                  t.status === "Answered" ? "bg-emerald-100 text-emerald-700" : "bg-gold-50 text-gold-dark"
                }`}
              >
                {t.status}
              </span>
            </div>
            <p className="mt-1 text-[11px] text-slate-400">{fmtDate(t.createdAt)}</p>
            {t.answer && (
              <p className="mt-2 rounded-lg bg-emerald-50 p-2.5 text-[13px] leading-relaxed text-emerald-900">
                <span className="font-bold">Teacher: </span>
                {t.answer}
              </p>
            )}
          </div>
        ))}
      </div>
    </section>
  );
}
