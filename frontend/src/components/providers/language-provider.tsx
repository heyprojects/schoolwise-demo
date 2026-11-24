"use client";

import { createContext, useContext, useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { createTranslator, intlLocale, localizeMessage, localeCookie, type Locale, type Translator } from "@/lib/i18n";

interface LanguageContextValue {
  locale: Locale;
  t: Translator;
  message: (value: string) => string;
  number: (value: number, options?: Intl.NumberFormatOptions) => string;
  date: (value: string, options?: Intl.DateTimeFormatOptions) => string;
  dateTime: (value: string) => string;
  changeLocale: (locale: Locale) => void;
  changing: boolean;
}

const LanguageContext = createContext<LanguageContextValue | null>(null);

export function LanguageProvider({ initialLocale, children }: { initialLocale: Locale; children: React.ReactNode }) {
  const [locale, setLocale] = useState(initialLocale);
  const [changing, startTransition] = useTransition();
  const router = useRouter();

  function changeLocale(next: Locale) {
    if (next === locale) return;
    document.cookie = `${localeCookie}=${next}; Path=/; Max-Age=31536000; SameSite=Lax${location.protocol === "https:" ? "; Secure" : ""}`;
    document.documentElement.lang = next;
    setLocale(next);
    startTransition(() => router.refresh());
  }

  return <LanguageContext.Provider value={{
    locale, t: createTranslator(locale), message: value => localizeMessage(locale, value),
    number: (value, options) => value.toLocaleString(intlLocale(locale), options),
    date: (value, options) => new Date(value).toLocaleDateString(intlLocale(locale), options),
    dateTime: value => new Date(value).toLocaleString(intlLocale(locale)),
    changeLocale, changing,
  }}>{children}</LanguageContext.Provider>;
}

export function useLanguage(): LanguageContextValue {
  const context = useContext(LanguageContext);
  if (!context) throw new Error("useLanguage requires LanguageProvider.");
  return context;
}
