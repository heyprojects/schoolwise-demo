"use client";

import { useLanguage } from "@/components/providers/language-provider";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { LogOut, School } from "lucide-react";
import { useAuth } from "@/components/providers/auth-provider";
import { Button, buttonVariants } from "@/components/ui/button";

export function SiteHeader() {
  const { t, locale, changeLocale, changing } = useLanguage();
  const { user, loading, logout } = useAuth();
  const router = useRouter();

  async function handleLogout() {
    await logout();
    router.push("/");
  }

  return (
    <header className="site-header">
      <div className="container-shell grid grid-cols-[1fr_auto] items-center gap-x-4 gap-y-3 py-4 lg:flex lg:min-h-[76px] lg:justify-between">
        <Link href="/" className="flex items-center gap-2.5 font-bold tracking-tight text-slate-950">
          <span className="brand-mark"><School size={21} strokeWidth={2.2} /></span>
          <span className="text-xl">SchoolWise</span>
        </Link>
        <nav className="col-span-2 row-start-2 flex flex-wrap items-center gap-x-6 gap-y-2 text-sm font-medium text-slate-600">
          <Link className="nav-link" href="/schools">{t("Explore schools")}</Link>
          <Link className="nav-link" href="/#how-it-works">{t("How it works")}</Link>
        </nav>
        <select
          aria-label={t("Language")}
          className="col-start-2 row-start-1 rounded-lg border border-slate-200 bg-white px-2 py-2 text-sm text-slate-700"
          value={locale}
          disabled={changing}
          onChange={event => changeLocale(event.target.value === "en" ? "en" : "el")}
        >
          <option value="el" lang="el">Ελληνικά</option>
          <option value="en" lang="en">English</option>
        </select>
        <div className="col-span-2 flex flex-wrap items-center gap-3">
          {loading ? (
            <span className="text-sm text-slate-500">{t("Loading…")}</span>
          ) : user ? (
            <>
              {user.role === "Admin" && <Link href="/admin/schools" className="text-sm font-semibold text-slate-700">{t("Admin")}</Link>}
              {user.role === "Admin" && <Link href="/admin/reviews" className="text-sm font-semibold text-slate-700">{t("Reviews")}</Link>}
              {user.role === "Parent" && <Link href="/my-reviews" className="text-sm font-semibold text-slate-700">{t("My Reviews")}</Link>}
              <span className="hidden text-sm text-slate-600 xl:inline">{t("Hi, {name}", { name: user.firstName })}</span>
              <Button variant="outline" size="sm" onClick={handleLogout} aria-label={t("Sign out")}>
                <LogOut size={15} /> <span className="hidden sm:inline">{t("Sign out")}</span>
              </Button>
            </>
          ) : (
            <>
              <Link href="/login" className="py-2 text-sm font-semibold text-slate-700">{t("Log in")}</Link>
              <Link className={buttonVariants({ size: "sm" })} href="/register">{t("Create account")}</Link>
            </>
          )}
        </div>
      </div>
    </header>
  );
}
