/**
 * Attractive flat-style SVG student illustration for the home page hero
 * (right side): a graduate student studying with a laptop, surrounded by
 * books, a certificate badge, a paper plane and sparkle accents — all in the
 * Scholaris brand palette (indigo #3F3D8F + amber #F0A731).
 */
export default function StudentHero() {
  return (
    <svg
      viewBox="0 0 560 520"
      role="img"
      aria-label="A student learning online with Scholaris"
      className="mx-auto w-full max-w-[520px] drop-shadow-sm"
    >
      {/* backdrop */}
      <circle cx="300" cy="250" r="215" fill="#EEEFF9" />
      <circle
        cx="300"
        cy="250"
        r="252"
        fill="none"
        stroke="#F0A731"
        strokeWidth="2"
        strokeDasharray="3 14"
        strokeLinecap="round"
        opacity="0.7"
      />
      <circle cx="88" cy="120" r="7" fill="#F0A731" />
      <circle cx="497" cy="86" r="5" fill="#3F3D8F" opacity="0.5" />
      <circle cx="62" cy="330" r="6" fill="#B3B6E5" />
      <path d="M468 316h18M477 307v18" stroke="#F0A731" strokeWidth="4" strokeLinecap="round" />
      <path d="M120 52h16M128 44v16" stroke="#B3B6E5" strokeWidth="4" strokeLinecap="round" />

      {/* paper plane with dotted trail */}
      <path d="M84 236 Q170 292 250 236" fill="none" stroke="#B3B6E5" strokeWidth="3" strokeDasharray="1 10" strokeLinecap="round" />
      <path d="M62 232 122 208 96 246 92 228z" fill="#F0A731" />
      <path d="M96 246 122 208 98 228z" fill="#D18B1A" />

      {/* ground shadow */}
      <ellipse cx="300" cy="468" rx="140" ry="18" fill="#E2E3F0" />

      {/* crossed legs */}
      <path d="M196 424 Q300 388 404 424 Q386 458 300 462 Q214 458 196 424 Z" fill="#312F73" />
      <ellipse cx="206" cy="440" rx="17" ry="11" fill="#D18B1A" />
      <ellipse cx="394" cy="440" rx="17" ry="11" fill="#D18B1A" />

      {/* torso / hoodie */}
      <path d="M232 300 Q300 268 368 300 L386 402 Q300 434 214 402 Z" fill="#3F3D8F" />
      <path d="M262 300 Q300 322 338 300 L330 318 Q300 336 270 318 Z" fill="#EEEFF9" opacity="0.9" />

      {/* arms */}
      <path d="M240 318 Q258 356 288 372" fill="none" stroke="#312F73" strokeWidth="22" strokeLinecap="round" />
      <path d="M360 318 Q342 356 312 372" fill="none" stroke="#312F73" strokeWidth="22" strokeLinecap="round" />
      <circle cx="292" cy="374" r="10" fill="#F2C6A0" />
      <circle cx="308" cy="374" r="10" fill="#F2C6A0" />

      {/* laptop */}
      <rect x="252" y="330" width="96" height="64" rx="7" fill="#1F2338" />
      <rect x="259" y="337" width="82" height="50" rx="4" fill="#EEEFF9" />
      <rect x="266" y="344" width="52" height="7" rx="3.5" fill="#3F3D8F" opacity="0.85" />
      <rect x="266" y="357" width="68" height="6" rx="3" fill="#B3B6E5" />
      <rect x="266" y="368" width="40" height="6" rx="3" fill="#F0A731" />
      <path d="M242 394h116l9 12H233z" fill="#3A3D52" />

      {/* head */}
      <circle cx="300" cy="238" r="40" fill="#F2C6A0" />
      <path d="M258 236 Q262 196 300 192 Q338 196 342 236 Q324 212 300 214 Q276 212 258 236 Z" fill="#2B2B3C" />
      <circle cx="286" cy="242" r="3.6" fill="#1F2338" />
      <circle cx="314" cy="242" r="3.6" fill="#1F2338" />
      <path d="M290 258 Q300 266 310 258" fill="none" stroke="#1F2338" strokeWidth="3" strokeLinecap="round" />
      <circle cx="276" cy="252" r="5" fill="#F5A9A0" opacity="0.55" />
      <circle cx="324" cy="252" r="5" fill="#F5A9A0" opacity="0.55" />

      {/* graduation cap */}
      <path d="M300 168 246 190 300 212 354 190z" fill="#312F73" />
      <circle cx="300" cy="190" r="3.5" fill="#F0A731" />
      <path d="M300 212 Q272 222 266 248" fill="none" stroke="#F0A731" strokeWidth="3.4" strokeLinecap="round" />
      <circle cx="265" cy="252" r="4.5" fill="#F0A731" />

      {/* book stack */}
      <rect x="106" y="382" width="96" height="19" rx="5" fill="#F0A731" />
      <rect x="112" y="401" width="84" height="17" rx="5" fill="#3F3D8F" />
      <rect x="118" y="418" width="72" height="16" rx="5" fill="#2FB4A8" />
      <rect x="116" y="386" width="76" height="4" rx="2" fill="#FFFFFF" opacity="0.85" />

      {/* certificate badge */}
      <circle cx="468" cy="182" r="36" fill="#F0A731" />
      <circle cx="468" cy="182" r="27" fill="#FFFFFF" />
      <path d="M456 182l8 9 17-18" fill="none" stroke="#3F3D8F" strokeWidth="5" strokeLinecap="round" strokeLinejoin="round" />
      <path d="M458 214l-6 26 16-9 16 9-6-26" fill="#D18B1A" />

      {/* plant */}
      <path d="M452 430l6-34h28l6 34z" fill="#2FB4A8" />
      <path d="M472 396 Q446 380 444 356 Q470 362 472 396z" fill="#35B7A0" />
      <path d="M474 396 Q500 376 504 352 Q476 360 474 396z" fill="#2AA396" />
      <ellipse cx="472" cy="434" rx="26" ry="5" fill="#E2E3F0" />

      {/* sparkles */}
      <path d="M190 128l4 10 10 4-10 4-4 10-4-10-10-4 10-4z" fill="#F0A731" />
      <path d="M416 268l3 8 8 3-8 3-3 8-3-8-8-3 8-3z" fill="#B3B6E5" />
    </svg>
  );
}
