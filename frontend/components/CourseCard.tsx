"use client";

import Link from "next/link";
import { fmtETB } from "@/lib/format";

export type CourseListItem = {
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
};

export function DiscountTag({ percent, className = "" }: { percent: number; className?: string }) {
  if (percent <= 0) return null;
  return (
    <span
      className={`rounded-full bg-gold px-2.5 py-1 text-xs font-extrabold text-white shadow-md ring-2 ring-white/70 ${className}`}
    >
      -{percent % 1 === 0 ? percent : percent.toFixed(1)}%
    </span>
  );
}

export function CourseCover({
  hue,
  subject,
  level,
  discountPercent = 0
}: {
  hue: number;
  subject: string;
  level: string;
  discountPercent?: number;
}) {
  return (
    <div
      className="relative h-36 overflow-hidden"
      style={{
        background: `linear-gradient(135deg, hsl(${hue}, 52%, 50%) 0%, hsl(${(hue + 42) % 360}, 58%, 36%) 100%)`
      }}
    >
      <svg
        className="absolute inset-0 h-full w-full opacity-25"
        viewBox="0 0 320 144"
        preserveAspectRatio="none"
        aria-hidden
      >
        <circle cx="270" cy="18" r="52" fill="#FFFFFF" opacity="0.5" />
        <circle cx="36" cy="130" r="38" fill="#FFFFFF" opacity="0.35" />
        <path d="M0 118 Q80 84 160 116 T320 104 V144 H0 Z" fill="#FFFFFF" opacity="0.22" />
      </svg>
      <svg
        className="absolute -bottom-1 right-3 h-24 w-24 opacity-90"
        viewBox="0 0 48 48"
        aria-hidden
      >
        <path d="M24 5 3 15.5 24 26l17-8.75V27h4V15.5L24 5z" fill="#FFFFFF" opacity="0.9" />
        <path d="M11 23.5v9c0 3.6 5.8 6.5 13 6.5s13-2.9 13-6.5v-9l-13 6.75L11 23.5z" fill="#FFFFFF" opacity="0.55" />
      </svg>
      <div className="absolute bottom-2.5 left-3 flex gap-1.5">
        <span className="rounded-full bg-black/25 px-2 py-0.5 text-[11px] font-semibold text-white backdrop-blur">
          {subject}
        </span>
        <span className="rounded-full bg-white/25 px-2 py-0.5 text-[11px] font-semibold text-white backdrop-blur">
          {level}
        </span>
      </div>
      <DiscountTag percent={discountPercent} className="absolute right-2.5 top-2.5" />
    </div>
  );
}

export default function CourseCard({ course }: { course: CourseListItem }) {
  const discounted = course.discountPercent > 0;
  return (
    <Link
      href={`/courses/${course.slug}`}
      className="group flex flex-col overflow-hidden rounded-2xl bg-white shadow-sm ring-1 ring-black/5 transition hover:-translate-y-1 hover:shadow-xl"
    >
      <CourseCover
        hue={course.coverHue}
        subject={course.subject}
        level={course.level}
        discountPercent={course.discountPercent}
      />
      <div className="flex flex-1 flex-col gap-2 p-4">
        <h3 className="line-clamp-2 font-bold leading-snug text-ink group-hover:text-brand">
          {course.title}
        </h3>
        <p className="line-clamp-2 text-[13px] leading-relaxed text-slate-500">{course.summary}</p>
        <div className="mt-auto space-y-2 pt-1 text-[13px] text-slate-600">
          <div className="flex items-center justify-between">
            <span className="font-semibold">{course.teacherName}</span>
            <span>{course.lessonCount} lessons</span>
          </div>
          <div className="flex items-center justify-between">
            <div className="flex items-baseline gap-2">
              <span className="text-base font-extrabold text-brand">{fmtETB(course.finalPrice)}</span>
              {discounted && (
                <span className="text-xs text-slate-400 line-through">{fmtETB(course.price)}</span>
              )}
            </div>
            <span className="rounded-full bg-brand-50 px-2.5 py-1 text-xs font-bold text-brand transition group-hover:bg-gold group-hover:text-white">
              {course.studentCount > 0 ? `${course.studentCount} enrolled` : "Be the first"}
            </span>
          </div>
        </div>
      </div>
    </Link>
  );
}
