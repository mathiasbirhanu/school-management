"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { Suspense, useEffect, useState } from "react";
import { api, ApiError } from "@/lib/api";
import { fmtETB } from "@/lib/format";
import { useAuth } from "@/lib/auth";
import type { CourseListItem } from "@/components/CourseCard";

type PaymentMethod = { code: string; name: string; description: string; isMock: boolean };

function CheckoutInner() {
  const params = useSearchParams();
  const courseId = params.get("course");
  const router = useRouter();
  const { user, ready } = useAuth();

  const [methods, setMethods] = useState<PaymentMethod[]>([]);
  const [selected, setSelected] = useState("");
  const [course, setCourse] = useState<CourseListItem | null>(null);
  const [phase, setPhase] = useState<"form" | "processing" | "done">("form");
  const [providerNote, setProviderNote] = useState("");
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (ready && !user && courseId)
      router.replace(`/login?next=${encodeURIComponent(`/checkout?course=${courseId}`)}`);
  }, [ready, user, courseId, router]);

  useEffect(() => {
    if (!courseId) return;
    api<PaymentMethod[]>("/api/payments/methods")
      .then((m) => {
        setMethods(m);
        setSelected(m[0]?.code ?? "");
      })
      .catch((e) => setError(e instanceof ApiError ? e.message : "Payment methods unavailable."));
    api<CourseListItem[]>("/api/courses", { noAuth: true })
      .then((list) => setCourse(list.find((c) => c.id === courseId) ?? null))
      .catch(() => {});
  }, [courseId]);

  const pay = async () => {
    if (!courseId || !selected) return;
    setPhase("processing");
    setError(null);
    try {
      const checkout = await api<{ orderId: string; providerRef: string; instructions: string }>(
        "/api/payments/checkout",
        { method: "POST", body: { courseId, methodCode: selected } }
      );
      setProviderNote(checkout.instructions);
      // Mock provider approval window — in Live mode the student completes payment on the
      // provider side and the webhook/confirm confirms the order (ADR-013).
      await new Promise((r) => setTimeout(r, 1600));
      await api("/api/payments/confirm", { method: "POST", body: { orderId: checkout.orderId } });
      setPhase("done");
    } catch (e) {
      setError(e instanceof ApiError ? e.message : "Payment could not be completed.");
      setPhase("form");
    }
  };

  if (!ready || !user) return <div className="mx-auto max-w-3xl px-4 py-20 text-center text-slate-500">Loading…</div>;

  return (
    <div className="mx-auto max-w-3xl px-4 py-10">
      <h1 className="text-3xl font-extrabold text-ink">Checkout</h1>
      <p className="mt-1 text-sm text-slate-500">You choose how to pay — Scholaris supports Ethiopia&apos;s popular methods.</p>

      {course && (
        <div className="mt-6 rounded-2xl bg-white p-5 shadow-sm ring-1 ring-black/5">
          <div className="flex items-center justify-between gap-4">
            <div>
              <h2 className="font-bold text-ink">{course.title}</h2>
              <p className="text-sm text-slate-500">{course.teacherName} · {course.level}</p>
            </div>
            <div className="text-right">
              <p className="text-xl font-extrabold text-brand">{fmtETB(course.finalPrice)}</p>
              {course.discountPercent > 0 && (
                <p className="text-xs text-slate-400 line-through">{fmtETB(course.price)}</p>
              )}
            </div>
          </div>
          {course.discountPercent > 0 && (
            <p className="mt-2 rounded-lg bg-gold-50 px-3 py-1.5 text-xs font-semibold text-gold-dark">
              🎉 Admin discount applied: -{course.discountPercent}% ({fmtETB(course.price - course.finalPrice)} off)
            </p>
          )}
        </div>
      )}

      {phase === "form" && (
        <>
          <h2 className="mt-8 font-extrabold text-ink">Choose your payment method</h2>
          <div className="mt-3 grid gap-3 sm:grid-cols-2">
            {methods.map((m) => (
              <button
                key={m.code}
                onClick={() => setSelected(m.code)}
                className={`flex items-start gap-3 rounded-2xl border-2 bg-white p-4 text-left transition ${
                  selected === m.code ? "border-brand shadow-md" : "border-black/5 hover:border-brand/30"
                }`}
              >
                <span
                  className={`mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full border-2 ${
                    selected === m.code ? "border-brand bg-brand" : "border-slate-300"
                  }`}
                >
                  {selected === m.code && <span className="h-2 w-2 rounded-full bg-white" />}
                </span>
                <span>
                  <span className="flex items-center gap-2 font-bold text-ink">
                    {m.name}
                    {m.isMock && (
                      <span className="rounded bg-brand-50 px-1.5 py-0.5 text-[10px] font-bold text-brand">
                        MOCK
                      </span>
                    )}
                  </span>
                  <span className="mt-0.5 block text-xs text-slate-500">{m.description}</span>
                </span>
              </button>
            ))}
          </div>

          {error && <p className="mt-4 rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-600">{error}</p>}

          <button
            onClick={pay}
            disabled={!selected || !courseId}
            className="mt-6 w-full rounded-full bg-gold px-6 py-3.5 text-base font-extrabold text-white shadow-lg shadow-gold/30 transition hover:bg-gold-dark disabled:opacity-50"
          >
            Pay {course ? fmtETB(course.finalPrice) : ""} →
          </button>
          <p className="mt-2 text-center text-[11px] text-slate-400">
            Mock mode: no real money moves. Real Tele Birr / Chapa keys plug into the same button later.
          </p>
        </>
      )}

      {phase === "processing" && (
        <div className="mt-10 rounded-2xl bg-white p-8 text-center shadow-sm ring-1 ring-black/5">
          <div className="mx-auto h-10 w-10 animate-spin rounded-full border-4 border-brand-100 border-t-brand" />
          <p className="mt-4 font-bold text-brand">Waiting for {selected.includes("telebirr") ? "Tele Birr" : "the provider"}…</p>
          <p className="mx-auto mt-2 max-w-md text-sm text-slate-500">{providerNote}</p>
        </div>
      )}

      {phase === "done" && (
        <div className="mt-10 rounded-2xl bg-white p-8 text-center shadow-sm ring-1 ring-black/5">
          <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-emerald-100 text-3xl">
            ✅
          </div>
          <h2 className="mt-4 text-xl font-extrabold text-emerald-600">Payment received — you&apos;re enrolled!</h2>
          <p className="mt-1 text-sm text-slate-500">A receipt and your course are ready in My learning.</p>
          {courseId && (
            <button
              onClick={() => router.push(`/learn/${courseId}`)}
              className="mt-5 rounded-full bg-brand px-6 py-3 text-sm font-bold text-white shadow hover:bg-brand-dark"
            >
              Start learning now →
            </button>
          )}
          <Link href="/student" className="mt-3 block text-xs font-semibold text-slate-400 hover:text-brand">
            or open My learning
          </Link>
        </div>
      )}
    </div>
  );
}

export default function CheckoutPage() {
  return (
    <Suspense fallback={<div className="mx-auto max-w-3xl px-4 py-20 text-center text-slate-500">Loading…</div>}>
      <CheckoutInner />
    </Suspense>
  );
}
