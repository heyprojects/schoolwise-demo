import "server-only";
import { cookies } from "next/headers";
import { createTranslator, localeCookie, resolveLocale } from "@/lib/i18n";

export async function getServerLanguage() {
  const locale = resolveLocale((await cookies()).get(localeCookie)?.value);
  return { locale, t: createTranslator(locale) };
}
