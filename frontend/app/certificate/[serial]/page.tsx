"use client";

import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import CertificateView, { type CertificateTemplateData } from "@/components/CertificateView";
import { api, ApiError } from "@/lib/api";
import { useAuth } from "@/lib/auth";

type CertificatePayload = {
  serial: string;
  studentName: string;
  courseTitle: string;
  issuedAt: string;
  template: CertificateTemplateData;
};

export default function CertificatePage() {
  const { serial } = useParams<{ serial: string }>();
  const { user } = useAuth();
  const [cert, setCert] = useState<CertificatePayload | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api<CertificatePayload>(`/api/certificates/${serial}`, { noAuth: true })
      .then(setCert)
      .catch((e) => setError(e instanceof ApiError ? e.message : "Certificate not found."));
  }, [serial]);

  return (
    <div className="mx-auto max-w-4xl px-4 py-10">
      <div className="mb-5 flex flex-wrap items-center justify-between gap-3 print-hide">
        <div>
          <h1 className="text-2xl font-extrabold text-ink">Course certificate</h1>
          <p className="text-sm text-slate-500">
            Anyone can verify this certificate with its serial — the wording and design are maintained
            by the Scholaris admin office.
          </p>
        </div>
        <button
          onClick={() => window.print()}
          className="rounded-full bg-brand px-5 py-2.5 text-sm font-bold text-white shadow hover:bg-brand-dark"
        >
          ⬇ Download / Print (PDF)
        </button>
      </div>

      {error && (
        <div className="rounded-2xl bg-rose-50 p-8 text-center text-rose-600">
          <p className="font-bold">{error}</p>
          <p className="mt-1 text-sm">Check the serial number and try again.</p>
        </div>
      )}

      {cert && (
        <>
          <CertificateView
            serial={cert.serial}
            studentName={cert.studentName}
            courseTitle={cert.courseTitle}
            issuedAt={cert.issuedAt}
            template={cert.template}
          />
          {user && (
            <p className="mt-4 text-center text-xs text-slate-400 print-hide">
              Awarded to {cert.studentName} · share the link to this page as proof of completion.
            </p>
          )}
        </>
      )}

      {!cert && !error && (
        <div className="h-[520px] animate-pulse rounded-2xl bg-white shadow" />
      )}
    </div>
  );
}
