import type { Metadata } from "next";
import "./globals.css";
import { Providers } from "@/components/providers";
import { SiteFooter } from "@/components/site-footer";
import { SiteHeader } from "@/components/site-header";

export const metadata: Metadata = {
  title: "SchoolWise | Find a school that fits",
  description: "Explore schools in Greece and find the right fit for your family.",
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en">
      <body>
        <Providers>
          <SiteHeader />
          <main className="min-h-[65vh]">{children}</main>
          <SiteFooter />
        </Providers>
      </body>
    </html>
  );
}
