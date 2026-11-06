"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import BrandedVideoPlayer from "@/components/BrandedVideoPlayer";
import AiNotesPanel from "@/components/AiNotesPanel";
import ExamPanel from "@/components/ExamPanel";
import QaPanel from "@/components/QaPanel";
import { api, ApiError } from "@/lib/api";

type Lesson = {
  id: string;
  title: string;
  type: string;
  durationMinutes: number;
  orderIndex: number;
  content: string | null;
  videoYoutubeId: string | null;
};
type Course = {
  courseId: string;
  title: string;
  subject: string;
  level: string;
  teacherName: string;
  modules: {
    id: string;
    title: string;
    orderIndex: number;
    topics: { id: string; title: string; orderIndex: number; lessons: Lesson[] }[];
  }[];
  completedLessonIds: string[];
  progressPercent: number;
  certificateSerial: string | null;
};

type Selection = { lesson: Lesson; topicId: string; topicTitle: string };

export default function LearnPage() {
  const { courseId } = useParams<{ courseId: string }>();
  const router = useRouter();
  const [course, setCourse] = useState<Course | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [selection, setSelection] = useState<Selection | null>(null);
  const [justCertified, setJustCertified] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const flat = useMemo(() => {
    const out: { lesson: Lesson; topicId: string; topicTitle: string }[] = [];
    course?.modules.forEach((m) =>
      m.topics.forEach((t) => t.lessons.forEach((l) => out.push({ lesson: l, topicId: t.id, topicTitle: t.title })))
    );
    return out;
  }, [course]);

  const load = useCallback(async () => {
    try {
      const data = await api<Course>(`/api/learn/${courseId}`);
      setCourse(data);
      setSelection((prev) => {
        if (prev) {
          const fresh = data.modules.flatMap((m) => m.topics).flatMap((t) => t.lessons).find((l) => l.id === prev.lesson.id);
          return fresh ? { lesson: fresh, topicId: prev.topicId, topicTitle: prev.topicTitle } : prev;
        }
        const first = data.modules.flatMap((m) => m.topics).flatMap((t) => t.lessons)[0];
        const topic = data.modules.flatMap((m) => m.topics).find((t) => t.lessons.some((l) => l.id === first?.id));
        return first && topic ? { lesson: first, topicId: topic.id, topicTitle: topic.title } : null;
      });
    } catch (e) {
      if (e instanceof ApiError && e.status === 402) {
        router.replace(`/checkout?course=${courseId}`);
        return;
      }
      setError(e instanceof ApiError ? e.message : "Could not open this course.");
    }
  }, [courseId, router]);

  useEffect(() => {
    void load();
  }, [load]);

  const completed = new Set(course?.completedLessonIds ?? []);

  const select = (item: Selection) => {
    setSelection(item);
    setJustCertified(null);
    window.scrollTo({ top: 0, behavior: "smooth" });
  };

  const step = (dir: 1 | -1) => {
    if (!selection) return;
    const i = flat.findIndex((f) => f.lesson.id === selection.lesson.id);
    const next = flat[i + dir];
    if (next) select(next);
  };

  const markComplete = async () => {
    if (!selection) return;
    setBusy(true);
    try {
      const res = await api<{ progressPercent: number; courseCompleted: boolean; certificateSerial: string | null }>(
        `/api/learn/lessons/${selection.lesson.id}/complete`,
        { method: "POST" }
      );
      setCourse((c) =>
        c
          ? {
              ...c,
              completedLessonIds: Array.from(new Set([...c.completedLessonIds, selection.lesson.id])),
              progressPercent: res.progressPercent,
              certificateSerial: res.certificateSerial ?? c.certificateSerial
            }
          : c
      );
      if (res.certificateSerial) setJustCertified(res.certificateSerial);
    } catch (e) {
      setError(e instanceof ApiError ? e.message : "Could not save progress.");
    } finally {
      setBusy(false);
    }
  };

  if (error && !course)
    return (
      <div className="mx-auto max-w-3xl px-4 py-20 text-center">
        <p className="font-bold text-brand">{error}</p>
        <Link href="/courses" className="mt-3 inline-block text-sm font-semibold text-gold-dark hover:underline">
          ← Back to catalog
        </Link>
      </div>
    );

  if (!course)
    return <div className="mx-auto max-w-6xl px-4 py-16"><div className="h-[70vh] animate-pulse rounded-3xl bg-white" /></div>;

  const currentIndex = selection ? flat.findIndex((f) => f.lesson.id === selection.lesson.id) : -1;

  return (
    <div className="mx-auto max-w-6xl px-4 py-8">
      {/* header */}
      <div className="mb-5">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h1 className="text-2xl font-extrabold text-ink">{course.title}</h1>
            <p className="text-sm text-slate-500">
              {course.subject} · {course.level} · {course.teacherName}
            </p>
          </div>
          <div className="w-full max-w-xs">
            <div className="flex justify-between text-xs font-semibold text-slate-500">
              <span>Progress</span>
              <span>{course.progressPercent}%</span>
            </div>
            <div className="mt-1 h-2.5 overflow-hidden rounded-full bg-brand-100">
              <div
                className="h-full rounded-full bg-gradient-to-r from-brand to-gold transition-all"
                style={{ width: `${course.progressPercent}%` }}
              />
            </div>
          </div>
        </div>

        {justCertified && (
          <div className="mt-4 rounded-2xl bg-gradient-to-r from-gold to-gold-dark p-4 text-white shadow-lg">
            <p className="font-extrabold">🎓 Congratulations! You completed the course.</p>
            <p className="mt-0.5 text-sm text-white/90">
              Your certificate was issued automatically.{" "}
              <Link href={`/certificate/${justCertified}`} className="font-bold underline">
                View &amp; print it →
              </Link>
            </p>
          </div>
        )}
        {!justCertified && course.certificateSerial && (
          <Link
            href={`/certificate/${course.certificateSerial}`}
            className="mt-4 inline-block rounded-full bg-gold-50 px-4 py-2 text-sm font-bold text-gold-dark ring-1 ring-gold/30 hover:bg-gold/10"
          >
            🎓 View your certificate →
          </Link>
        )}
      </div>

      <div className="grid gap-6 lg:grid-cols-[300px_1fr]">
        {/* curriculum sidebar */}
        <aside className="nice-scroll max-h-[calc(100vh-2rem)] overflow-y-auto rounded-2xl bg-white p-4 shadow-sm ring-1 ring-black/5 lg:sticky lg:top-20">
          {course.modules.map((m) => (
            <div key={m.id} className="mb-4">
              <p className="text-xs font-extrabold uppercase tracking-wide text-slate-400">
                Module {m.orderIndex}: {m.title}
              </p>
              {m.topics.map((t) => (
                <div key={t.id} className="mt-2">
                  <p className="text-[13px] font-bold text-brand">{t.title}</p>
                  <ul className="mt-1 space-y-0.5">
                    {t.lessons.map((l) => {
                      const isDone = completed.has(l.id);
                      const isActive = selection?.lesson.id === l.id;
                      return (
                        <li key={l.id}>
                          <button
                            onClick={() => select({ lesson: l, topicId: t.id, topicTitle: t.title })}
                            className={`flex w-full items-center gap-2 rounded-lg px-2 py-1.5 text-left text-[13px] transition ${
                              isActive ? "bg-brand font-semibold text-white" : "text-slate-600 hover:bg-brand-50"
                            }`}
                          >
                            <span
                              className={`flex h-4.5 w-4.5 h-[18px] w-[18px] shrink-0 items-center justify-center rounded-full text-[10px] ${
                                isDone ? "bg-emerald-500 text-white" : isActive ? "bg-white/25 text-white" : "bg-brand-50 text-brand"
                              }`}
                              aria-hidden
                            >
                              {isDone ? "✓" : l.type === "Video" ? "▶" : "•"}
                            </span>
                            <span className="min-w-0 flex-1 truncate">{l.title}</span>
                            <span className={`text-[10px] ${isActive ? "text-white/70" : "text-slate-400"}`}>
                              {l.durationMinutes}m
                            </span>
                          </button>
                        </li>
                      );
                    })}
                  </ul>
                </div>
              ))}
            </div>
          ))}
        </aside>

        {/* lesson area */}
        <div className="space-y-6">
          {selection && (
            <>
              <section className="rounded-2xl bg-white p-5 shadow-sm ring-1 ring-black/5">
                <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
                  <h2 className="text-lg font-extrabold text-ink">{selection.lesson.title}</h2>
                  <div className="flex items-center gap-2">
                    <button
                      onClick={() => step(-1)}
                      disabled={currentIndex <= 0}
                      className="rounded-full border border-black/10 px-3 py-1.5 text-xs font-bold text-slate-600 hover:bg-slate-50 disabled:opacity-40"
                    >
                      ← Prev
                    </button>
                    <button
                      onClick={() => step(1)}
                      disabled={currentIndex < 0 || currentIndex >= flat.length - 1}
                      className="rounded-full border border-black/10 px-3 py-1.5 text-xs font-bold text-slate-600 hover:bg-slate-50 disabled:opacity-40"
                    >
                      Next →
                    </button>
                  </div>
                </div>

                {selection.lesson.type === "Video" && selection.lesson.videoYoutubeId ? (
                  <BrandedVideoPlayer videoId={selection.lesson.videoYoutubeId} title={selection.lesson.title} />
                ) : (
                  <div className="notes-body text-slate-700">{selection.lesson.content}</div>
                )}

                <div className="mt-5 flex items-center gap-3 border-t border-black/5 pt-4">
                  {completed.has(selection.lesson.id) ? (
                    <span className="rounded-full bg-emerald-100 px-4 py-2 text-sm font-bold text-emerald-700">
                      ✓ Completed
                    </span>
                  ) : (
                    <button
                      onClick={markComplete}
                      disabled={busy}
                      className="rounded-full bg-emerald-600 px-5 py-2 text-sm font-bold text-white shadow transition hover:bg-emerald-700 disabled:opacity-50"
                    >
                      {busy ? "Saving…" : "Mark as complete"}
                    </button>
                  )}
                  <span className="text-xs text-slate-400">
                    Complete every lesson to unlock your certificate automatically.
                  </span>
                </div>
              </section>

              <AiNotesPanel lessonId={selection.lesson.id} notes={selection.lesson.content ?? ""} />

              <ExamPanel topicId={selection.topicId} topicTitle={selection.topicTitle} />

              <QaPanel courseId={course.courseId} lessonId={selection.lesson.id} />
            </>
          )}
        </div>
      </div>
    </div>
  );
}
