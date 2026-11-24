"use client";

import { useLanguage } from "@/components/providers/language-provider";

import Link from "next/link";
import Image from "next/image";
import { useQuery } from "@tanstack/react-query";
import { ArrowRight, BookOpenCheck, MapPin, Search, ShieldCheck } from "lucide-react";
import { useState } from "react";
import { SchoolCard } from "@/components/school-card";
import { Button, buttonVariants } from "@/components/ui/button";
import { searchSchools } from "@/lib/api";
import type { PageResult, SchoolListItem } from "@/lib/types";

export default function HomeClient({ initialSchools }: { initialSchools: PageResult<SchoolListItem> }) {
  const { t } = useLanguage();
  const [keyword, setKeyword] = useState("");
  const [city, setCity] = useState("");
  const featured = useQuery({
    queryKey: ["schools", "featured"],
    queryFn: () => searchSchools({ pageSize: 3, sortBy: "name" }),
    initialData: initialSchools,
  });

  function submitSearch(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const params = new URLSearchParams();
    if (keyword.trim()) params.set("keyword", keyword.trim());
    if (city.trim()) params.set("city", city.trim());
    window.location.href = `/schools${params.size ? `?${params.toString()}` : ""}`;
  }

  return (
    <>
      <section className="hero-section">
        <div className="container-shell">
          <div className="editorial-hero">
            <div className="editorial-hero-copy">
              <p className="eyebrow">{t("Your family’s next chapter")}</p>
              <h1><span>{t("Small steps.")}</span><em>{t("Big choices.")}</em></h1>
              <p className="hero-intro">{t("Explore schools across Greece, compare the details that matter, and make your next step with confidence.")}</p>
              <a href="#school-search" className="editorial-text-link">{t("Find the right school for your child.")}<ArrowRight size={18} /></a>
            </div>
            <figure className="editorial-illustration">
              <span className="illustration-note">{t("A place to grow")}</span>
              <Image src="/illustrations/school-garden.svg" width={480} height={465} alt="" priority />
              <figcaption>{t("Illustration, not a school photograph.")}</figcaption>
            </figure>
          </div>
          <form id="school-search" onSubmit={submitSearch} className="search-panel" aria-labelledby="school-search-title">
            <div className="search-panel-heading">
              <h2 id="school-search-title">{t("Let’s find your school")}</h2>
              <p>{t("Start with a school name or location.")}</p>
            </div>
            <div className="search-panel-fields">
              <label htmlFor="home-keyword">
                <span>{t("School name or area")}</span>
                <span className="search-field"><Search size={18} aria-hidden="true" /><input id="home-keyword" value={keyword} onChange={(event) => setKeyword(event.target.value)} placeholder={t("School name or area")} /></span>
              </label>
              <label htmlFor="home-city">
                <span>{t("City")}</span>
                <span className="search-field"><MapPin size={18} aria-hidden="true" /><input id="home-city" value={city} onChange={(event) => setCity(event.target.value)} placeholder={t("City, e.g. Athens")} /></span>
              </label>
              <Button type="submit" className="search-submit">{t("Search schools")}<ArrowRight size={17} /></Button>
            </div>
          </form>
          <div className="editorial-values">
            <span><MapPin size={19} />{t("Explore at your pace")}</span>
            <span><BookOpenCheck size={19} />{t("Thoughtful school information")}</span>
            <span><ShieldCheck size={19} />{t("Moderated parent reviews")}</span>
          </div>
        </div>
      </section>

      <section className="container-shell py-16 sm:py-20">
        <div className="section-heading">
          <div><p className="eyebrow">{t("A place to begin")}</p><h2>{t("Schools to explore")}</h2><p>{t("Browse a growing selection of schools and find the details you need.")}</p></div>
          <Link className={buttonVariants({ variant: "outline" })} href="/schools">{t("Explore all schools")}{" "}<ArrowRight size={16} /></Link>
        </div>
        {featured.isLoading ? <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">{[1, 2, 3].map((item) => <div key={item} className="skeleton-card" />)}</div> : null}
        {featured.isError ? <p className="rounded-xl bg-amber-50 p-4 text-sm text-amber-900">{t("School listings could not be loaded. Please try again later.")}</p> : null}
        {featured.data?.items.length ? <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">{featured.data.items.map((school) => <SchoolCard key={school.id} school={school} />)}</div> : null}
        {featured.data && featured.data.items.length === 0 ? <p className="rounded-xl bg-slate-50 p-6 text-slate-600">{t("No schools are listed yet. Check back soon.")}</p> : null}
      </section>

      <section id="how-it-works" className="how-it-works py-16 sm:py-20">
        <div className="container-shell">
          <div className="mx-auto max-w-2xl text-center"><p className="eyebrow justify-center">{t("How SchoolWise helps")}</p><h2 className="editorial-title mt-3 text-3xl">{t("A better-informed choice starts here.")}</h2><p className="mt-4 leading-7 text-slate-600">{t("Compare practical school details and published parent experiences to help guide your decision.")}</p></div>
          <div className="mt-11 grid gap-5 md:grid-cols-3">
            <article className="benefit-card"><span className="benefit-icon"><Search size={20} /></span><h3>{t("Explore your options")}</h3><p>{t("Search by area, school type, and tuition range to focus on what matters to your family.")}</p></article>
            <article className="benefit-card"><span className="benefit-icon"><BookOpenCheck size={20} /></span><h3>{t("Compare the details")}</h3><p>{t("See school information, location, contact details, and photos in one place.")}</p></article>
            <article className="benefit-card"><span className="benefit-icon"><ShieldCheck size={20} /></span><h3>{t("Read parent experiences")}</h3><p>{t("Read published reviews and school ratings, or submit your own experience for moderation. Reviews are not independently verified.")}</p></article>
          </div>
        </div>
      </section>
    </>
  );
}
