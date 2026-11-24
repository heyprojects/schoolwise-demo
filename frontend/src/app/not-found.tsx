"use client";

import Link from "next/link";
import { useLanguage } from "@/components/providers/language-provider";

export default function NotFound() {
  const { t } = useLanguage();
  return <div className="container-shell py-16">
    <div className="empty-state">
      <h1>{t("Page not found")}</h1>
      <p>{t("The page you requested does not exist.")}</p>
      <Link href="/schools" className="nav-link">{t("Back to schools")}</Link>
    </div>
  </div>;
}
