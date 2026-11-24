"use client";

import { useLanguage } from "@/components/providers/language-provider";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { BookOpen, LogOut } from "lucide-react";
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
      <div className="container-shell header-inner">
        <Link href="/" className="site-brand">
          <span className="brand-mark"><BookOpen size={25} strokeWidth={1.6} /></span>
          <span>SchoolWise<span className="brand-dot">.</span></span>
        </Link>
        <nav className="header-nav">
          <Link className="nav-link" href="/schools">{t("Explore schools")}</Link>
          <Link className="nav-link" href="/#how-it-works">{t("How it works")}</Link>
        </nav>
        <select
          aria-label={t("Language")}
          className="header-language"
          value={locale}
          disabled={changing}
          onChange={event => changeLocale(event.target.value === "en" ? "en" : "el")}
        >
          <option value="el" lang="el">Ελληνικά</option>
          <option value="en" lang="en">English</option>
        </select>
        <div className="header-account">
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
              <Link className={buttonVariants({ size: "lg" })} href="/register">{t("Create account")}</Link>
            </>
          )}
        </div>
      </div>
    </header>
  );
}
