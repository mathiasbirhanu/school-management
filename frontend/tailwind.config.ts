import type { Config } from "tailwindcss";

const config: Config = {
  content: ["./app/**/*.{ts,tsx}", "./components/**/*.{ts,tsx}"],
  theme: {
    extend: {
      colors: {
        brand: {
          DEFAULT: "#3F3D8F",
          dark: "#312F73",
          50: "#EEEFF9",
          100: "#D9DBF2",
          200: "#B3B6E5"
        },
        gold: {
          DEFAULT: "#F0A731",
          dark: "#D18B1A",
          50: "#FDF3E0"
        },
        ink: "#1F2338"
      },
      fontFamily: {
        sans: ["Inter", "Segoe UI", "system-ui", "-apple-system", "sans-serif"],
        serif: ["Georgia", "Times New Roman", "serif"]
      },
      keyframes: {
        floaty: {
          "0%, 100%": { transform: "translateY(0)" },
          "50%": { transform: "translateY(-14px)" }
        },
        sway: {
          "0%, 100%": { transform: "rotate(-2deg)" },
          "50%": { transform: "rotate(2deg)" }
        },
        fadeUp: {
          "0%": { opacity: "0", transform: "translateY(10px)" },
          "100%": { opacity: "1", transform: "translateY(0)" }
        }
      },
      animation: {
        floaty: "floaty 4s ease-in-out infinite",
        sway: "sway 5s ease-in-out infinite",
        fadeUp: "fadeUp 0.5s ease-out both"
      }
    }
  },
  plugins: []
};

export default config;
