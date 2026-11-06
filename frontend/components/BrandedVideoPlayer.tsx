"use client";

import { useEffect, useRef, useState } from "react";
import Logo from "./Logo";
import { fmtTime } from "@/lib/format";

declare global {
  interface Window {
    YT?: any;
    onYouTubeIframeAPIReady?: () => void;
  }
}

function loadYouTubeApi(): Promise<any> {
  if (typeof window === "undefined") return Promise.reject(new Error("no window"));
  if (window.YT?.Player) return Promise.resolve(window.YT);
  return new Promise((resolve) => {
    const previous = window.onYouTubeIframeAPIReady;
    window.onYouTubeIframeAPIReady = () => {
      previous?.();
      resolve(window.YT);
    };
    if (!document.getElementById("scholaris-yt-api")) {
      const script = document.createElement("script");
      script.id = "scholaris-yt-api";
      script.src = "https://www.youtube.com/iframe_api";
      document.head.appendChild(script);
    }
  });
}

/**
 * Scholaris Branded Player — the student should never notice the stream comes from YouTube.
 *
 * De-branding measures (code-level):
 *  • controls=0, disablekb=1, modestbranding=1, rel=0, iv_load_policy=3 — no YT chrome, no
 *    related grid, no title overlay.
 *  • Full custom control bar in brand colors (play/seek/volume/fullscreen).
 *  • Scholaris logo pinned TOP-LEFT over the video (brand requirement).
 *  • Scholaris watermark pinned BOTTOM-RIGHT (covers the corner where a YT watermark would sit).
 *  • Right-click disabled; playback auto-pauses when the tab loses focus (anti screen-recording
 *    deterrent, part of the three-layer protection design).
 */
export default function BrandedVideoPlayer({ videoId, title }: { videoId: string; title: string }) {
  const hostRef = useRef<HTMLDivElement>(null);
  const wrapRef = useRef<HTMLDivElement>(null);
  const playerRef = useRef<any>(null);
  const [ready, setReady] = useState(false);
  const [playing, setPlaying] = useState(false);
  const [current, setCurrent] = useState(0);
  const [duration, setDuration] = useState(0);
  const [muted, setMuted] = useState(false);
  const [volume, setVolume] = useState(1);

  useEffect(() => {
    let interval: ReturnType<typeof setInterval> | undefined;
    let cancelled = false;

    loadYouTubeApi()
      .then((YT) => {
        if (cancelled || !hostRef.current) return;
        playerRef.current = new YT.Player(hostRef.current, {
          videoId,
          playerVars: {
            controls: 0,
            disablekb: 1,
            modestbranding: 1,
            rel: 0,
            iv_load_policy: 3,
            fs: 0,
            playsinline: 1,
            origin: window.location.origin
          },
          events: {
            onReady: () => {
              setReady(true);
              setDuration(playerRef.current.getDuration?.() ?? 0);
            },
            onStateChange: (e: any) => setPlaying(e.data === 1)
          }
        });
        interval = setInterval(() => {
          const p = playerRef.current;
          if (p?.getCurrentTime) setCurrent(p.getCurrentTime());
          if (p?.getDuration) setDuration(p.getDuration());
        }, 500);
      })
      .catch(() => setReady(false));

    const onVisibility = () => {
      if (document.hidden && playerRef.current?.pauseVideo) playerRef.current.pauseVideo();
    };
    document.addEventListener("visibilitychange", onVisibility);

    return () => {
      cancelled = true;
      document.removeEventListener("visibilitychange", onVisibility);
      if (interval) clearInterval(interval);
      try {
        playerRef.current?.destroy?.();
      } catch {
        /* ignore */
      }
    };
  }, [videoId]);

  const togglePlay = () => {
    const p = playerRef.current;
    if (!p) return;
    if (playing) p.pauseVideo();
    else p.playVideo();
  };

  const seek = (seconds: number) => {
    playerRef.current?.seekTo?.(seconds, true);
    setCurrent(seconds);
  };

  const toggleMute = () => {
    const p = playerRef.current;
    if (!p) return;
    if (muted) p.unMute();
    else p.mute();
    setMuted(!muted);
  };

  const changeVolume = (v: number) => {
    setVolume(v);
    setMuted(v === 0);
    playerRef.current?.setVolume?.(Math.round(v * 100));
  };

  const fullscreen = () => {
    const el = wrapRef.current;
    if (!el) return;
    if (document.fullscreenElement) void document.exitFullscreen();
    else void el.requestFullscreen();
  };

  return (
    <div
      ref={wrapRef}
      className="group relative aspect-video w-full select-none overflow-hidden rounded-2xl bg-ink shadow-xl ring-1 ring-black/10"
      onContextMenu={(e) => e.preventDefault()}
    >
      {/* The YouTube iframe is rendered into this host div by the IFrame API */}
      <div ref={hostRef} className="pointer-events-none absolute inset-0 h-full w-full" />

      {/* Top brand bar — logo pinned top-left per product spec */}
      <div className="pointer-events-none absolute inset-x-0 top-0 flex items-center justify-between bg-gradient-to-b from-black/70 via-black/30 to-transparent px-4 pb-8 pt-3">
        <Logo light small />
        <span className="max-w-[60%] truncate text-sm font-semibold text-white/90">{title}</span>
      </div>

      {/* Bottom-right brand watermark (over the corner YouTube normally brands) */}
      <div className="pointer-events-none absolute bottom-14 right-3 rounded-md bg-brand/85 px-2 py-0.5 text-[11px] font-bold tracking-wide text-white shadow">
        Scholaris
      </div>

      {/* Center play affordance */}
      {ready && !playing && (
        <button
          onClick={togglePlay}
          aria-label="Play"
          className="absolute inset-0 m-auto flex h-16 w-16 items-center justify-center rounded-full bg-gold text-white shadow-2xl transition hover:scale-105"
        >
          <svg width="26" height="26" viewBox="0 0 24 24" fill="currentColor" aria-hidden>
            <path d="M8 5v14l11-7z" />
          </svg>
        </button>
      )}

      {/* Custom control bar */}
      <div className="absolute inset-x-0 bottom-0 bg-gradient-to-t from-black/80 to-transparent px-3 pb-2.5 pt-6 opacity-90 transition group-hover:opacity-100">
        <input
          type="range"
          min={0}
          max={duration || 0}
          step={1}
          value={current}
          onChange={(e) => seek(Number(e.target.value))}
          className="h-1.5 w-full cursor-pointer appearance-none rounded-full bg-white/25 accent-[#F0A731]"
          aria-label="Seek"
        />
        <div className="mt-1.5 flex items-center gap-3 text-white">
          <button onClick={togglePlay} aria-label={playing ? "Pause" : "Play"} className="hover:text-gold">
            {playing ? (
              <svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor">
                <path d="M6 5h4v14H6zM14 5h4v14h-4z" />
              </svg>
            ) : (
              <svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor">
                <path d="M8 5v14l11-7z" />
              </svg>
            )}
          </button>
          <button onClick={toggleMute} aria-label={muted ? "Unmute" : "Mute"} className="hover:text-gold">
            {muted ? (
              <svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor">
                <path d="M16.5 12 20 8.5 18.5 7 15 10.5 11.5 7 10 8.5 13.5 12 10 15.5 11.5 17 15 13.5 18.5 17 20 15.5zM4 9v6h4l5 4V5L8 9z" />
              </svg>
            ) : (
              <svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor">
                <path d="M3 9v6h4l5 4V5L7 9zm13.5 3a4.5 4.5 0 0 0-2.5-4v8a4.5 4.5 0 0 0 2.5-4z" />
              </svg>
            )}
          </button>
          <input
            type="range"
            min={0}
            max={1}
            step={0.05}
            value={muted ? 0 : volume}
            onChange={(e) => changeVolume(Number(e.target.value))}
            className="h-1 w-20 cursor-pointer appearance-none rounded-full bg-white/25 accent-[#F0A731]"
            aria-label="Volume"
          />
          <span className="text-xs tabular-nums text-white/85">
            {fmtTime(current)} / {fmtTime(duration)}
          </span>
          <span className="ml-auto rounded bg-white/15 px-1.5 py-0.5 text-[10px] font-bold tracking-wider text-white/80">
            HD
          </span>
          <button onClick={fullscreen} aria-label="Fullscreen" className="hover:text-gold">
            <svg width="19" height="19" viewBox="0 0 24 24" fill="currentColor">
              <path d="M7 14H5v5h5v-2H7zM5 10h2V7h3V5H5zm12 7h-3v2h5v-5h-2zM14 5v2h3v3h2V5z" />
            </svg>
          </button>
        </div>
      </div>

      {!ready && (
        <div className="absolute inset-0 flex items-center justify-center bg-ink text-sm text-white/70">
          Loading lesson video…
        </div>
      )}
    </div>
  );
}
