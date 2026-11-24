"use client";

import { useLanguage } from "@/components/providers/language-provider";
import Link from "next/link";

export function SiteFooter() {
  const { t } = useLanguage();
  return (
    <footer className="site-footer">
      <div className="container-shell flex flex-col gap-5 py-8 text-sm text-slate-500 sm:flex-row sm:items-center sm:justify-between">
        <div className="max-w-md">
          <Link href="/" className="site-brand">SchoolWise<span className="brand-dot">.</span></Link>
          <p className="mt-3 text-xs leading-6">© {new Date().getFullYear()}{" "}{t("SchoolWise. Helping families make confident choices.")}</p>
        </div>
        <div className="flex flex-wrap gap-5">
          <Link href="/schools" className="hover:text-slate-900">{t("Explore schools")}</Link>
          <Link href="/login" className="hover:text-slate-900">{t("School admin")}</Link>
        </div>
      </div>
    </footer>
  );
}
