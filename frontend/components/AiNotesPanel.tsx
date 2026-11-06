"use client";

import { useState } from "react";
import { api, ApiError } from "@/lib/api";

type LessonSummary = { summary: string; keyPoints: string[]; elaborated: string };

/** AI study aid generated from the teacher's notes — shown ephemerally, never stored. */
export default function AiNotesPanel({ lessonId, notes }: { lessonId: string; notes: string }) {
  const [summary, setSummary] = useState<LessonSummary | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const summarize = async () => {
    setBusy(true);
    setError(null);
    try {
      setSummary(await api<LessonSummary>("/api/ai/notes/summarize", { method: "POST", body: { lessonId } }));
    } catch (e) {
      setError(e instanceof ApiError ? e.message : "AI summary is unavailable right now.");
    } finally {
      setBusy(false);
    }
  };

  if (!notes.trim()) return null;

  return (
    <section className="rounded-2xl bg-white p-5 shadow-sm ring-1 ring-black/5">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h3 className="font-extrabold text-brand">AI study summary</h3>
        <span className="rounded-full bg-brand-50 px-2.5 py-1 text-[11px] font-semibold text-brand">
          From your teacher&apos;s notes
        </span>
      </div>

      {!summary ? (
        <>
          <p className="mt-1 text-[13px] text-slate-500">
            Let AI re-explain this lesson in simpler words, pull out the key points, and elaborate on
            the tricky parts.
          </p>
          <button
            onClick={summarize}
            disabled={busy}
            className="mt-3 rounded-full bg-brand px-5 py-2 text-sm font-bold text-white shadow transition hover:bg-brand-dark disabled:opacity-50"
          >
            {busy ? "Thinking…" : "✨ Summarize & elaborate"}
          </button>
        </>
      ) : (
        <div className="mt-3 space-y-4">
          <div>
            <h4 className="text-xs font-bold uppercase tracking-wide text-slate-400">Summary</h4>
            <p className="mt-1 text-sm leading-relaxed text-ink">{summary.summary}</p>
          </div>
          {summary.keyPoints.length > 0 && (
            <div>
              <h4 className="text-xs font-bold uppercase tracking-wide text-slate-400">Key points</h4>
              <ul className="mt-1 space-y-1 text-sm text-ink">
                {summary.keyPoints.map((p, i) => (
                  <li key={i} className="flex gap-2">
                    <span className="mt-1.5 h-1.5 w-1.5 shrink-0 rounded-full bg-gold" aria-hidden />
                    {p}
                  </li>
                ))}
              </ul>
            </div>
          )}
          <div>
            <h4 className="text-xs font-bold uppercase tracking-wide text-slate-400">Explained more clearly</h4>
            <p className="notes-body mt-1">{summary.elaborated}</p>
          </div>
          <div className="flex items-center justify-between border-t border-black/5 pt-3">
            <p className="text-[11px] text-slate-400">
              Generated fresh from the notes each time — not stored anywhere.
            </p>
            <button onClick={summarize} disabled={busy} className="text-xs font-bold text-brand hover:underline">
              Regenerate
            </button>
          </div>
        </div>
      )}

      {error && <p className="mt-3 rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-600">{error}</p>}
    </section>
  );
}
