"use client";

import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import { currentUser, login as loginRequest, logout as logoutRequest, refreshSession, register as registerRequest } from "@/lib/api";
import type { User } from "@/lib/types";

interface AuthContextValue {
  user: User | null;
  loading: boolean;
  sessionError: string | null;
  login: (email: string, password: string) => Promise<void>;
  register: (input: { firstName: string; lastName: string; email: string; password: string }) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);
  const [sessionError, setSessionError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    refreshSession()
      .then(() => currentUser())
      .then((current) => {
        if (active) setUser(current);
      })
      .catch((error: unknown) => {
        const status = (error as { response?: { status?: number } }).response?.status;
        if (active && status !== 401) {
          setSessionError("Could not reach SchoolWise. Check that the API is running.");
        }
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    setSessionError(null);
    setUser(await loginRequest(email, password));
  }, []);

  const register = useCallback(async (input: { firstName: string; lastName: string; email: string; password: string }) => {
    await registerRequest(input);
  }, []);

  const logout = useCallback(async () => {
    try {
      await logoutRequest();
    } finally {
      setUser(null);
    }
  }, []);

  const value = useMemo(() => ({ user, loading, sessionError, login, register, logout }), [
    user, loading, sessionError, login, register, logout,
  ]);
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const value = useContext(AuthContext);
  if (!value) throw new Error("useAuth must be used within AuthProvider.");
  return value;
}
