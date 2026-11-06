import Logo from "./Logo";
import { fmtDate } from "@/lib/format";

export type CertificateTemplateData = {
  title: string;
  subtitle: string;
  bodyTemplate: string;
  footerNote: string;
  signatoryName: string;
  signatoryTitle: string;
  accentHex: string;
};

/**
 * The Scholaris certificate. Wording, signatory and accent color all come from the
 * admin-maintained CertificateTemplate; placeholders are filled at render time:
 * {{StudentName}} {{CourseTitle}} {{Date}} {{Serial}}.
 */
export default function CertificateView({
  serial,
  studentName,
  courseTitle,
  issuedAt,
  template
}: {
  serial: string;
  studentName: string;
  courseTitle: string;
  issuedAt: string | Date;
  template: CertificateTemplateData;
}) {
  const accent = template.accentHex || "#3F3D8F";
  const fill = (line: string) =>
    line
      .replace(/\{\{StudentName\}\}/g, studentName)
      .replace(/\{\{CourseTitle\}\}/g, courseTitle)
      .replace(/\{\{Date\}\}/g, fmtDate(issuedAt))
      .replace(/\{\{Serial\}\}/g, serial);

  const lines = template.bodyTemplate.split("\n");

  return (
    <div
      className="print-area relative overflow-hidden rounded-xl bg-white p-2 shadow-2xl"
      style={{ border: `3px solid ${accent}` }}
    >
      <div className="relative border-[6px] px-6 py-8 sm:px-10" style={{ borderColor: "#F0A731" }}>
        {/* subtle dotted background */}
        <div
          className="pointer-events-none absolute inset-0 opacity-[0.35]"
          style={{
            backgroundImage: `radial-gradient(${accent}22 1.2px, transparent 1.2px)`,
            backgroundSize: "22px 22px"
          }}
          aria-hidden
        />

        <div className="relative flex flex-col items-center text-center">
          <div className="scale-90">
            <Logo />
          </div>
          <h1 className="mt-4 font-serif text-2xl font-bold sm:text-4xl" style={{ color: accent }}>
            {template.title}
          </h1>
          <p className="mt-1 text-xs font-semibold uppercase tracking-[0.3em] text-gold-dark">
            {template.subtitle}
          </p>

          <div className="mt-6 max-w-2xl space-y-2 font-serif text-[15px] leading-relaxed text-slate-700 sm:text-lg">
            {lines.map((line, i) => {
              const filled = fill(line);
              if (line.includes("{{StudentName}}")) {
                return (
                  <p
                    key={i}
                    className="my-3 border-b-2 pb-1 font-serif text-2xl font-bold italic sm:text-4xl"
                    style={{ color: accent, borderColor: "#F0A731" }}
                  >
                    {studentName}
                  </p>
                );
              }
              if (!filled.trim()) return <div key={i} className="h-2" />;
              return <p key={i}>{filled}</p>;
            })}
          </div>

          <div className="mt-8 flex w-full max-w-2xl items-end justify-between gap-6">
            {/* seal */}
            <svg width="86" height="98" viewBox="0 0 86 98" aria-hidden className="shrink-0">
              <path d="M43 62 33 96l10-6 10 6z" fill="#D18B1A" />
              <circle cx="43" cy="43" r="30" fill="#F0A731" />
              <circle cx="43" cy="43" r="23" fill="none" stroke="#FFFFFF" strokeWidth="2" strokeDasharray="3 4" />
              <path d="M31 43l8 9 16-17" fill="none" stroke="#FFFFFF" strokeWidth="4.5" strokeLinecap="round" strokeLinejoin="round" />
            </svg>

            <div className="text-center">
              <p className="text-[11px] uppercase tracking-widest text-slate-400">Awarded on</p>
              <p className="font-serif text-base text-slate-700">{fmtDate(issuedAt)}</p>
            </div>

            <div className="min-w-[140px] text-center">
              <div className="mx-auto mb-1 w-full border-t-2 border-slate-300" />
              <p className="text-sm font-bold text-slate-800">{template.signatoryName}</p>
              <p className="text-xs text-slate-500">{template.signatoryTitle}</p>
            </div>
          </div>

          <p className="mt-6 text-[11px] text-slate-400">{fill(template.footerNote)}</p>
          <p className="mt-1 font-mono text-[11px] tracking-wider text-slate-500">{serial}</p>
        </div>
      </div>
    </div>
  );
}
