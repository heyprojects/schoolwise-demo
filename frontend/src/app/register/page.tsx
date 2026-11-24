"use client";

import { useLanguage } from "@/components/providers/language-provider";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { z } from "zod";
import { useState } from "react";
import { useAuth } from "@/components/providers/auth-provider";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { getErrorMessage } from "@/lib/api";

const schema = z.object({
  firstName: z.string().trim().min(1, "Enter your first name.").max(100, "Use at most 100 characters."),
  lastName: z.string().trim().min(1, "Enter your last name.").max(100, "Use at most 100 characters."),
  email: z.email("Enter a valid email address."),
  password: z.string().min(10, "Use at least 10 characters.").max(128, "Use at most 128 characters.")
    .regex(/[A-Z]/, "Include an uppercase letter.")
    .regex(/[a-z]/, "Include a lowercase letter.")
    .regex(/[0-9]/, "Include a number."),
});

export default function RegisterPage() {
  const { t, message } = useLanguage();
  const { register } = useAuth();
  const router = useRouter();
  const [form, setForm] = useState({ firstName: "", lastName: "", email: "", password: "" });
  const [error, setError] = useState("");
  const [submitting, setSubmitting] = useState(false);

  async function submit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const parsed = schema.safeParse(form);
    if (!parsed.success) { setError(parsed.error.issues[0]?.message ?? "Check the form and try again."); return; }
    setSubmitting(true); setError("");
    try {
      await register(parsed.data);
      router.push("/login?registered=1");
    } catch (reason) {
      setError(getErrorMessage(reason));
    } finally {
      setSubmitting(false);
    }
  }

  return <div className="auth-page"><div className="auth-card"><div className="text-center"><p className="eyebrow justify-center">{t("Join the community")}</p><h1>{t("Create your account")}</h1><p>{t("Save time as you explore schools for your family.")}</p></div><form className="mt-8 space-y-4" onSubmit={submit}><div className="grid gap-4 sm:grid-cols-2"><div className="space-y-2"><Label htmlFor="firstName">{t("First name")}</Label><Input id="firstName" autoComplete="given-name" required value={form.firstName} onChange={(event) => setForm({ ...form, firstName: event.target.value })} /></div><div className="space-y-2"><Label htmlFor="lastName">{t("Last name")}</Label><Input id="lastName" autoComplete="family-name" required value={form.lastName} onChange={(event) => setForm({ ...form, lastName: event.target.value })} /></div></div><div className="space-y-2"><Label htmlFor="email">{t("Email address")}</Label><Input id="email" type="email" autoComplete="email" required value={form.email} onChange={(event) => setForm({ ...form, email: event.target.value })} /></div><div className="space-y-2"><Label htmlFor="password">{t("Password")}</Label><Input id="password" type="password" autoComplete="new-password" required value={form.password} onChange={(event) => setForm({ ...form, password: event.target.value })} /><p className="text-xs text-slate-500">{t("At least 10 characters, including uppercase, lowercase, and a number.")}</p></div>{error && <p role="alert" className="rounded-lg bg-rose-50 p-3 text-sm text-rose-800">{message(error)}</p>}<Button type="submit" className="h-11 w-full" disabled={submitting}>{submitting ? t("Creating account…") : t("Create account")}</Button></form><p className="mt-6 text-center text-sm text-slate-500">{t("Already have an account?")}{" "}<Link href="/login" className="font-semibold text-teal-700 hover:underline">{t("Log in")}</Link></p></div></div>;
}
