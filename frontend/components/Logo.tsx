export default function Logo({ light = false, small = false }: { light?: boolean; small?: boolean }) {
  return (
    <span className="inline-flex items-center gap-2">
      <svg width={small ? 24 : 30} height={small ? 24 : 30} viewBox="0 0 48 48" aria-hidden>
        <path d="M24 5 3 15.5 24 26l17-8.75V27h4V15.5L24 5z" fill="#F0A731" />
        <path
          d="M11 23.5v9c0 3.6 5.8 6.5 13 6.5s13-2.9 13-6.5v-9l-13 6.75L11 23.5z"
          fill={light ? "#FFFFFF" : "#3F3D8F"}
        />
        <rect x="22" y="12" width="4" height="14" rx="1" fill={light ? "#FFFFFF" : "#3F3D8F"} opacity="0.25" />
      </svg>
      <span
        className={`font-extrabold tracking-tight ${light ? "text-white" : "text-brand"} ${
          small ? "text-lg" : "text-xl"
        }`}
      >
        Scholaris
      </span>
    </span>
  );
}
