"use client";

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useState } from "react";
import { AuthProvider } from "@/components/providers/auth-provider";
import { LanguageProvider } from "@/components/providers/language-provider";
import type { Locale } from "@/lib/i18n";

export function Providers({ children, locale }: { children: React.ReactNode; locale: Locale }) {
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: { staleTime: 30_000, retry: 1, refetchOnWindowFocus: false },
        },
      }),
  );

  return (
    <LanguageProvider initialLocale={locale}>
      <QueryClientProvider client={queryClient}>
        <AuthProvider>{children}</AuthProvider>
      </QueryClientProvider>
    </LanguageProvider>
  );
}
