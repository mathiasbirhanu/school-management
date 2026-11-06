import type { Metadata } from "next";
import "./globals.css";
import { AuthProvider } from "@/lib/auth";
import Nav from "@/components/Nav";
import BalloonAnnouncement from "@/components/BalloonAnnouncement";
import Logo from "@/components/Logo";

export const metadata: Metadata = {
  title: "Scholaris — Learn without limits",
  description:
    "Scholaris online academy: K-12 and university courses with teacher notes, videos, AI practice tests and certificates. Pay with Tele Birr and other Ethiopian payment methods."
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body className="flex min-h-screen flex-col">
        <AuthProvider>
          <Nav />
          <main className="flex-1">{children}</main>
          <footer className="mt-auto border-t border-black/5 bg-white print-hide">
            <div className="mx-auto flex max-w-6xl flex-col items-center justify-between gap-3 px-4 py-6 text-sm text-slate-500 sm:flex-row">
              <Logo small />
              <p>K-12 · College · University — learn, practice with AI, earn certificates.</p>
              <p>© {new Date().getFullYear()} Scholaris</p>
            </div>
          </footer>
          <BalloonAnnouncement />
        </AuthProvider>
      </body>
    </html>
  );
}
