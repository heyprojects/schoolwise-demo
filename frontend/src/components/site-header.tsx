"use client";

import { useLanguage } from "@/components/providers/language-provider";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { LogOut, School } from "lucide-react";
import { useAuth } from "@/components/providers/auth-provider";
import { Button, buttonVariants } from "@/components/ui/button";

const languages = [
  { value: "el", label: "ΕΛ", name: "Ελληνικά" },
  { value: "en", label: "EN", name: "English" },
] as const;

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
      <div className="container-shell grid grid-cols-[1fr_auto] items-center gap-x-4 gap-y-3 py-4 lg:grid lg:min-h-[76px] lg:grid-cols-[auto_minmax(0,1fr)_6.75rem_18rem] lg:gap-x-6 lg:py-0 xl:grid-cols-[auto_minmax(0,1fr)_6.75rem_24rem]">
        <Link href="/" className="flex items-center gap-2.5 font-bold tracking-tight text-slate-950">
          <span className="brand-mark"><School size={21} strokeWidth={2.2} /></span>
          <span className="text-xl">SchoolWise</span>
        </Link>
        <nav className="col-span-2 row-start-2 flex flex-wrap items-center gap-x-6 gap-y-2 text-sm font-medium text-slate-600 lg:col-start-2 lg:col-span-1 lg:row-start-1">
          <Link className="nav-link" href="/schools">{t("Explore schools")}</Link>
          <Link className="nav-link" href="/#how-it-works">{t("How it works")}</Link>
        </nav>
        <div
          role="group"
          aria-label={t("Language")}
          aria-busy={changing}
          className="col-start-2 row-start-1 inline-flex w-[6.75rem] shrink-0 justify-self-end rounded-lg border border-slate-200 bg-white p-1 lg:col-start-3 lg:row-start-1 lg:justify-self-center"
        >
          {languages.map(language => (
            <button
              key={language.value}
              type="button"
              lang={language.value}
              aria-label={language.name}
              title={language.name}
              aria-pressed={locale === language.value}
              disabled={changing}
              onClick={() => changeLocale(language.value)}
              className={`h-12 w-12 shrink-0 cursor-pointer rounded-md p-0 text-sm font-semibold transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-teal-700 disabled:cursor-wait disabled:opacity-60 ${locale === language.value ? "bg-teal-700 text-white" : "text-slate-600 hover:bg-teal-50 hover:text-teal-800"}`}
            >
              {language.label}
            </button>
          ))}
        </div>
        <div className="col-span-2 flex flex-wrap items-center gap-3 lg:col-start-4 lg:col-span-1 lg:row-start-1 lg:flex-nowrap lg:justify-end xl:gap-3">
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
