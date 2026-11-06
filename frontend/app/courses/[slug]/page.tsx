"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { CourseCover, DiscountTag } from "@/components/CourseCard";
import { api, ApiError } from "@/lib/api";
import { fmtETB } from "@/lib/format";
import { useAuth } from "@/lib/auth";

type Detail = {
  id: string;
  slug: string;
  title: string;
  summary: string;
  subject: string;
  level: string;
  teacherName: string;
  price: number;
  discountPercent: number;
  finalPrice: number;
  coverHue: number;
  lessonCount: number;
  studentCount: number;
  enrolled: boolean;
  modules: {
    id: string;
    title: string;
    orderIndex: number;
    topics: {
      id: string;
      title: string;
      concepts: string;
      orderIndex: number;
      lessons: { id: string; title: string; type: string; durationMinutes: number; orderIndex: number }[];
    }[];
  }[];
};

export default function CourseDetailPage() {
  const { slug } = useParams<{ slug: string }>();
  const router = useRouter();
  const { user } = useAuth();
  const [course, setCourse] = useState<Detail | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [open, setOpen] = useState<Set<string>>(new Set());

  useEffect(() => {
    api<Detail>(`/api/courses/${slug}`)
      .then((c) => {
        setCourse(c);
        setOpen(new Set(c.modules.slice(0, 1).map((m) => m.id)));
      })
      .catch((e) => setError(e instanceof ApiError ? e.message : "Course not found."));
  }, [slug]);

  const toggle = (id: string) => {
    setOpen((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const goLearn = () => {
    if (!course) return;
    router.push(user ? `/learn/${course.id}` : `/login?next=${encodeURIComponent(`/learn/${course.id}`)}`);
  };

  if (error)
    return (
      <div className="mx-auto max-w-3xl px-4 py-20 text-center">
        <p className="text-lg font-bold text-brand">{error}</p>
        <Link href="/courses" className="mt-3 inline-block text-sm font-semibold text-gold-dark hover:underline">
          ← Back to catalog
        </Link>
      </div>
    );

  if (!course)
    return (
      <div className="mx-auto max-w-5xl px-4 py-16">
        <div className="h-96 animate-pulse rounded-3xl bg-white" />
      </div>
    );

  return (
    <div className="mx-auto max-w-6xl px-4 py-10">
      <div className="grid gap-8 lg:grid-cols-[1fr_340px]">
        {/* Left: info + curriculum */}
        <div>
          <CourseCover
            hue={course.coverHue}
            subject={course.subject}
            level={course.level}
            discountPercent={course.discountPercent}
          />
          <div className="mt-5 flex flex-wrap items-center gap-3">
            <h1 className="text-3xl font-extrabold text-ink">{course.title}</h1>
            {course.enrolled && (
              <span className="rounded-full bg-emerald-100 px-3 py-1 text-xs font-bold text-emerald-700">
                ✓ Enrolled
              </span>
            )}
          </div>
          <p className="mt-2 text-[15px] leading-relaxed text-slate-600">{course.summary}</p>
          <p className="mt-3 text-sm text-slate-500">
            Taught by <span className="font-bold text-slate-700">{course.teacherName}</span> ·{" "}
            {course.lessonCount} lessons · {course.studentCount} enrolled
          </p>

          <h2 className="mt-8 text-xl font-extrabold text-ink">Curriculum</h2>
          <div className="mt-3 space-y-3">
            {course.modules.map((m) => (
              <div key={m.id} className="overflow-hidden rounded-2xl bg-white shadow-sm ring-1 ring-black/5">
                <button
                  onClick={() => toggle(m.id)}
                  className="flex w-full items-center justify-between px-5 py-4 text-left"
                >
                  <span className="font-bold text-brand">
                    Module {m.orderIndex}: {m.title}
                  </span>
                  <span className="text-slate-400">{open.has(m.id) ? "▾" : "▸"}</span>
                </button>
                {open.has(m.id) && (
                  <div className="space-y-4 border-t border-black/5 px-5 py-4">
                    {m.topics.map((t) => (
                      <div key={t.id}>
                        <p className="font-semibold text-ink">Topic: {t.title}</p>
                        {t.concepts && <p className="mt-0.5 text-xs text-slate-400">Concepts: {t.concepts}</p>}
                        <ul className="mt-2 space-y-1.5">
                          {t.lessons.map((l) => (
                            <li key={l.id} className="flex items-center gap-2 text-sm text-slate-600">
                              <span
                                className="flex h-6 w-6 items-center justify-center rounded-full bg-brand-50 text-[11px] text-brand"
                                aria-hidden
                              >
                                {l.type === "Video" ? "▶" : "📄"}
                              </span>
                              {l.title}
                              <span className="text-xs text-slate-400">· {l.durationMinutes} min</span>
                            </li>
                          ))}
                        </ul>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            ))}
          </div>
        </div>

        {/* Right: purchase card */}
        <aside className="lg:sticky lg:top-24 lg:self-start">
          <div className="rounded-2xl bg-white p-6 shadow-lg ring-1 ring-black/5">
            <div className="flex items-center justify-between">
              <div className="flex items-baseline gap-2">
                <span className="text-3xl font-extrabold text-brand">{fmtETB(course.finalPrice)}</span>
                {course.discountPercent > 0 && (
                  <span className="text-sm text-slate-400 line-through">{fmtETB(course.price)}</span>
                )}
              </div>
              <DiscountTag percent={course.discountPercent} />
            </div>
            {course.discountPercent > 0 && (
              <p className="mt-1 text-xs font-semibold text-gold-dark">
                You save {fmtETB(course.price - course.finalPrice)} — admin-set discount 🎉
              </p>
            )}

            {course.enrolled ? (
              <button
                onClick={goLearn}
                className="mt-4 w-full rounded-full bg-emerald-600 px-5 py-3 text-sm font-bold text-white shadow transition hover:bg-emerald-700"
              >
                Continue learning →
              </button>
            ) : (
              <button
                onClick={() => router.push(`/checkout?course=${course.id}`)}
                className="mt-4 w-full rounded-full bg-gold px-5 py-3 text-sm font-bold text-white shadow-lg shadow-gold/30 transition hover:bg-gold-dark"
              >
                Enroll now · pay your way
              </button>
            )}

            <ul className="mt-4 space-y-2 text-[13px] text-slate-600">
              <li>✓ Lifetime access to notes &amp; videos</li>
              <li>✓ AI study summaries for every lesson</li>
              <li>✓ AI practice test on every topic (fresh each attempt)</li>
              <li>✓ Automatic certificate on completion</li>
              <li>✓ Ask the course teacher questions anytime</li>
            </ul>
          </div>
        </aside>
      </div>
    </div>
  );
}
