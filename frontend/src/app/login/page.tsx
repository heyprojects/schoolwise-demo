"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { z } from "zod";
import { useState } from "react";
import { useAuth } from "@/components/providers/auth-provider";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { getErrorMessage } from "@/lib/api";

const schema = z.object({ email: z.email(), password: z.string().min(1) });

export default function LoginPage() {
  const { login } = useAuth();
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [submitting, setSubmitting] = useState(false);

  async function submit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const parsed = schema.safeParse({ email, password });
    if (!parsed.success) { setError(parsed.error.issues[0]?.message ?? "Enter a valid email and password."); return; }
    setSubmitting(true); setError("");
    try {
      await login(parsed.data.email, parsed.data.password);
      router.push("/schools");
    } catch (reason) {
      setError(getErrorMessage(reason));
    } finally {
      setSubmitting(false);
    }
  }

  return <div className="auth-page"><div className="auth-card"><div className="text-center"><p className="eyebrow justify-center">Welcome back</p><h1>Log in to SchoolWise</h1><p>Continue exploring schools for your family.</p></div><form className="mt-8 space-y-5" onSubmit={submit}><div className="space-y-2"><Label htmlFor="email">Email address</Label><Input id="email" type="email" autoComplete="email" required value={email} onChange={(event) => setEmail(event.target.value)} /></div><div className="space-y-2"><Label htmlFor="password">Password</Label><Input id="password" type="password" autoComplete="current-password" required value={password} onChange={(event) => setPassword(event.target.value)} /></div>{error && <p role="alert" className="rounded-lg bg-rose-50 p-3 text-sm text-rose-800">{error}</p>}<Button type="submit" className="h-11 w-full" disabled={submitting}>{submitting ? "Signing in…" : "Log in"}</Button></form><p className="mt-6 text-center text-sm text-slate-500">New to SchoolWise? <Link href="/register" className="font-semibold text-teal-700 hover:underline">Create an account</Link></p></div></div>;
}
