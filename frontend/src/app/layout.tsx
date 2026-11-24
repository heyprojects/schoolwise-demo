import type { Metadata } from "next";
import "./globals.css";
import { Providers } from "@/components/providers";
import { SiteFooter } from "@/components/site-footer";
import { SiteHeader } from "@/components/site-header";
import { getServerLanguage } from "@/lib/server-language";

export async function generateMetadata(): Promise<Metadata> {
  const { t } = await getServerLanguage();
  return {
    title: t("SchoolWise | Find a school that fits"),
    description: t("Explore schools in Greece and find the right fit for your family."),
  };
}

export default async function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  const { locale } = await getServerLanguage();
  return (
    <html lang={locale}>
      <body>
        <Providers locale={locale}>
          <SiteHeader />
          <main className="min-h-[65vh]">{children}</main>
          <SiteFooter />
        </Providers>
      </body>
    </html>
  );
}
