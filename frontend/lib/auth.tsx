"use client";

import { createContext, useContext, useEffect, useState, type ReactNode } from "react";
import { api, clearSession, getSessionUser, saveSession, type SessionUser } from "./api";

type AuthContextValue = {
  user: SessionUser | null;
  ready: boolean;
  login: (email: string, password: string) => Promise<SessionUser>;
  register: (fullName: string, email: string, password: string) => Promise<SessionUser>;
  logout: () => void;
};

const AuthContext = createContext<AuthContextValue>(null as unknown as AuthContextValue);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<SessionUser | null>(null);
  const [ready, setReady] = useState(false);

  useEffect(() => {
    let alive = true;
    // Rehydrate the client-only session after mount (localStorage is unavailable during SSR).
    Promise.resolve().then(() => {
      if (!alive) return;
      setUser(getSessionUser());
      setReady(true);
    });
    return () => {
      alive = false;
    };
  }, []);

  const login = async (email: string, password: string) => {
    const data = await api<{ accessToken: string; refreshToken: string; user: SessionUser }>(
      "/api/auth/login",
      { method: "POST", body: { email, password } }
    );
    saveSession(data.accessToken, data.refreshToken, data.user);
    setUser(data.user);
    return data.user;
  };

  const register = async (fullName: string, email: string, password: string) => {
    const data = await api<{ accessToken: string; refreshToken: string; user: SessionUser }>(
      "/api/auth/register",
      { method: "POST", body: { fullName, email, password } }
    );
    saveSession(data.accessToken, data.refreshToken, data.user);
    setUser(data.user);
    return data.user;
  };

  const logout = () => {
    clearSession();
    setUser(null);
  };

  return (
    <AuthContext.Provider value={{ user, ready, login, register, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export const useAuth = () => useContext(AuthContext);
