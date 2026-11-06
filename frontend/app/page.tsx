"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import StudentHero from "@/components/StudentHero";
import CourseCard, { type CourseListItem } from "@/components/CourseCard";
import { api } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { homeFor } from "@/lib/format";

const steps = [
  {
    icon: "💳",
    title: "Enroll in minutes",
    text: "Pick your course and pay with Tele Birr, CBE Birr, M-Pesa or card — you choose the method."
  },
  {
    icon: "📚",
    title: "Learn with real teachers",
    text: "Structured notes and video lessons organized into modules and topics, at your own pace."
  },
  {
    icon: "🤖",
    title: "Practice with AI",
    text: "Every topic has an AI practice test: fresh questions each attempt, graded instantly."
  },
  {
    icon: "🎓",
    title: "Earn your certificate",
    text: "Finish all lessons and Scholaris awards a verifiable certificate automatically."
  }
];

export default function HomePage() {
  const [courses, setCourses] = useState<CourseListItem[] | null>(null);
  const [offline, setOffline] = useState(false);
  const { user } = useAuth();

  useEffect(() => {
    api<CourseListItem[]>("/api/courses", { noAuth: true })
      .then(setCourses)
      .catch(() => setOffline(true));
  }, []);

  const teacherCount = courses ? new Set(courses.map((c) => c.teacherName)).size : 0;
  const studentCount = courses ? courses.reduce((s, c) => s + c.studentCount, 0) : 0;
  const featured = (courses ?? []).slice(0, 6);

  return (
    <div>
      {/* ── Hero: content left · student illustration right ── */}
      <section className="relative overflow-hidden bg-gradient-to-b from-brand-50 to-[#F7F7FB]">
        <div className="mx-auto grid max-w-6xl items-center gap-10 px-4 py-12 lg:grid-cols-2 lg:py-16">
          <div className="animate-fadeUp">
            <span className="inline-flex items-center gap-2 rounded-full bg-white px-3 py-1 text-xs font-bold text-brand shadow-sm ring-1 ring-brand/10">
              🎓 K-12 · College · University
            </span>
            <h1 className="mt-4 text-4xl font-extrabold leading-tight tracking-tight text-ink sm:text-5xl">
              Every subject. Every level.{" "}
              <span className="text-brand">One learning home.</span>
            </h1>
            <p className="mt-4 max-w-lg text-[15px] leading-relaxed text-slate-600">
              Scholaris brings Ethiopia&apos;s students and great teachers together — structured
              notes, focused video lessons, AI practice tests for every topic, and a certificate
              when you finish. Pay with Tele Birr and other local payment methods.
            </p>
            <div className="mt-6 flex flex-wrap gap-3">
              <Link
                href="/courses"
                className="rounded-full bg-brand px-6 py-3 text-sm font-bold text-white shadow-lg shadow-brand/25 transition hover:bg-brand-dark"
              >
                Browse courses
              </Link>
              <Link
                href={user ? homeFor(user.role) : "/register"}
                className="rounded-full bg-gold px-6 py-3 text-sm font-bold text-white shadow-lg shadow-gold/30 transition hover:bg-gold-dark"
              >
                {user ? "Go to my dashboard" : "Start learning today"}
              </Link>
            </div>
            <div className="mt-8 flex gap-8 text-sm">
              <div>
                <p className="text-2xl font-extrabold text-brand">{courses?.length ?? "–"}</p>
                <p className="text-slate-500">Courses</p>
              </div>
              <div>
                <p className="text-2xl font-extrabold text-brand">{teacherCount || "–"}</p>
                <p className="text-slate-500">Teachers</p>
              </div>
              <div>
                <p className="text-2xl font-extrabold text-brand">{studentCount || "–"}</p>
                <p className="text-slate-500">Enrollments</p>
              </div>
            </div>
          </div>

          <div className="animate-fadeUp">
            <StudentHero />
          </div>
        </div>
      </section>

      {/* ── Featured courses ── */}
      <section className="mx-auto max-w-6xl px-4 py-12">
        <div className="mb-6 flex items-end justify-between">
          <div>
            <h2 className="text-2xl font-extrabold text-ink">Featured courses</h2>
            <p className="mt-1 text-sm text-slate-500">
              Admin-curated catalog — watch for the gold balloon, it announces discounted courses.
            </p>
          </div>
          <Link href="/courses" className="text-sm font-bold text-brand hover:underline">
            See all →
          </Link>
        </div>

        {offline && (
          <div className="rounded-2xl border border-dashed border-brand/30 bg-white p-8 text-center">
            <p className="font-bold text-brand">The Scholaris API is not reachable right now.</p>
            <p className="mt-1 text-sm text-slate-500">
              Start the backend (<code className="rounded bg-slate-100 px-1">docker compose up</code>) and
              refresh this page.
            </p>
          </div>
        )}

        <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
          {courses === null && !offline
            ? Array.from({ length: 6 }).map((_, i) => (
                <div key={i} className="h-72 animate-pulse rounded-2xl bg-white shadow-sm" />
              ))
            : featured.map((c) => <CourseCard key={c.id} course={c} />)}
        </div>
      </section>

      {/* ── How it works ── */}
      <section className="bg-white py-12">
        <div className="mx-auto max-w-6xl px-4">
          <h2 className="text-center text-2xl font-extrabold text-ink">How Scholaris works</h2>
          <div className="mt-8 grid gap-5 sm:grid-cols-2 lg:grid-cols-4">
            {steps.map((s, i) => (
              <div key={s.title} className="relative rounded-2xl bg-brand-50/60 p-5 ring-1 ring-brand/10">
                <span className="absolute right-4 top-3 text-3xl font-black text-brand/10">{i + 1}</span>
                <div className="text-2xl" aria-hidden>
                  {s.icon}
                </div>
                <h3 className="mt-2 font-bold text-brand">{s.title}</h3>
                <p className="mt-1 text-[13px] leading-relaxed text-slate-600">{s.text}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ── Demo accounts helper ── */}
      <section className="mx-auto max-w-6xl px-4 py-10">
        <div className="rounded-2xl border border-dashed border-gold/50 bg-gold-50/50 p-5 text-sm">
          <p className="font-bold text-gold-dark">Demo accounts (seeded data)</p>
          <div className="mt-2 grid gap-1 text-slate-600 sm:grid-cols-3">
            <p>
              Admin: <code className="rounded bg-white px-1">admin@scholaris.et</code> / Admin@123
            </p>
            <p>
              Teacher: <code className="rounded bg-white px-1">sara.tesfaye@scholaris.et</code> / Teacher@123
            </p>
            <p>
              Student: <code className="rounded bg-white px-1">abi@student.scholaris.et</code> / Student@123
            </p>
          </div>
        </div>
      </section>
    </div>
  );
}
