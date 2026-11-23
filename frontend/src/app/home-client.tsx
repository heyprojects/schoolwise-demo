"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { ArrowRight, BadgeCheck, BookOpenCheck, HeartHandshake, Search, ShieldCheck, Sparkles } from "lucide-react";
import { useState } from "react";
import { SchoolCard } from "@/components/school-card";
import { Button, buttonVariants } from "@/components/ui/button";
import { searchSchools } from "@/lib/api";
import type { PageResult, SchoolListItem } from "@/lib/types";

export default function HomeClient({ initialSchools }: { initialSchools: PageResult<SchoolListItem> }) {
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
        <div className="container-shell relative grid gap-12 py-16 md:grid-cols-[1.05fr_.95fr] md:items-center md:py-24">
          <div className="relative z-10">
            <div className="eyebrow"><Sparkles size={14} /> A clearer way to choose</div>
            <h1 className="mt-6 max-w-2xl text-5xl font-bold leading-[1.05] tracking-[-.045em] text-slate-950 sm:text-6xl">
              Find the <span className="text-teal-700">right school</span> for your child.
            </h1>
            <p className="mt-6 max-w-xl text-lg leading-8 text-slate-600">
              Explore schools across Greece, compare the details that matter, and make your next step with confidence.
            </p>
            <form onSubmit={submitSearch} className="search-panel mt-9">
              <label className="flex min-w-0 flex-1 items-center gap-3">
                <Search size={19} className="shrink-0 text-teal-700" />
                <input value={keyword} onChange={(event) => setKeyword(event.target.value)} placeholder="School name or area" aria-label="School name or area" />
              </label>
              <span className="hidden h-9 w-px bg-slate-200 sm:block" />
              <label className="flex min-w-0 flex-1 items-center gap-3">
                <input value={city} onChange={(event) => setCity(event.target.value)} placeholder="City, e.g. Athens" aria-label="City" />
              </label>
              <Button type="submit" className="h-12 shrink-0 rounded-xl px-5">Search schools</Button>
            </form>
            <div className="mt-5 flex flex-wrap items-center gap-x-5 gap-y-2 text-sm text-slate-500">
              <span className="flex items-center gap-1.5"><ShieldCheck size={16} className="text-teal-700" /> Thoughtful school information</span>
              <span className="flex items-center gap-1.5"><BadgeCheck size={16} className="text-teal-700" /> Parent reviews coming soon</span>
            </div>
          </div>
          <div className="hero-art relative mx-auto w-full max-w-[500px]">
            <div className="hero-orbit" />
            <div className="hero-photo">
              <div className="flex h-full flex-col justify-between rounded-[28px] bg-gradient-to-br from-teal-100 via-emerald-50 to-amber-100 p-7">
                <div className="flex items-center justify-between">
                  <span className="rounded-full bg-white/80 px-3 py-1.5 text-xs font-semibold text-teal-800">Your family’s next chapter</span>
                  <span className="rounded-full bg-white/80 p-2 text-teal-700"><BookOpenCheck size={18} /></span>
                </div>
                <div>
                  <div className="mb-4 flex -space-x-3">
                    <span className="avatar-dot bg-amber-300">P</span><span className="avatar-dot bg-rose-200">A</span><span className="avatar-dot bg-sky-200">M</span>
                  </div>
                  <p className="max-w-[280px] text-3xl font-bold leading-tight tracking-tight text-slate-900">Every child deserves a place to thrive.</p>
                  <p className="mt-3 text-sm text-slate-600">Start with the questions that matter to you.</p>
                </div>
              </div>
            </div>
            <div className="floating-note"><HeartHandshake size={18} className="text-teal-700" /><span><b>Family first</b><small>Explore at your pace</small></span></div>
          </div>
        </div>
      </section>

      <section className="container-shell py-16 sm:py-20">
        <div className="section-heading">
          <div><p className="eyebrow">A place to begin</p><h2>Schools to explore</h2><p>Browse a growing selection of schools and find the details you need.</p></div>
          <Link className={buttonVariants({ variant: "outline" })} href="/schools">Explore all schools <ArrowRight size={16} /></Link>
        </div>
        {featured.isLoading ? <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">{[1, 2, 3].map((item) => <div key={item} className="skeleton-card" />)}</div> : null}
        {featured.isError ? <p className="rounded-xl bg-amber-50 p-4 text-sm text-amber-900">School listings could not be loaded. Please try again later.</p> : null}
        {featured.data?.items.length ? <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">{featured.data.items.map((school) => <SchoolCard key={school.id} school={school} />)}</div> : null}
        {featured.data && featured.data.items.length === 0 ? <p className="rounded-xl bg-slate-50 p-6 text-slate-600">No schools are listed yet. Check back soon.</p> : null}
      </section>

      <section id="how-it-works" className="bg-[#f3f7f6] py-16 sm:py-20">
        <div className="container-shell">
          <div className="mx-auto max-w-2xl text-center"><p className="eyebrow justify-center">How SchoolWise helps</p><h2 className="mt-3 text-3xl font-bold tracking-tight text-slate-950">A better-informed choice starts here.</h2><p className="mt-4 leading-7 text-slate-600">Compare practical details today. Verified parent perspectives are part of what we are building next.</p></div>
          <div className="mt-11 grid gap-5 md:grid-cols-3">
            <article className="benefit-card"><span className="benefit-icon"><Search size={20} /></span><h3>Explore your options</h3><p>Search by area, school type, and tuition range to focus on what matters to your family.</p></article>
            <article className="benefit-card"><span className="benefit-icon"><BookOpenCheck size={20} /></span><h3>Compare the details</h3><p>See school information, location, contact details, and photos in one place.</p></article>
            <article className="benefit-card"><span className="benefit-icon"><ShieldCheck size={20} /></span><h3>Trust the journey</h3><p>Verified parent reviews are coming in a future phase. They are not yet available on SchoolWise.</p></article>
          </div>
        </div>
      </section>
    </>
  );
}
