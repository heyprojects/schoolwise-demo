"use client";

import { useQuery } from "@tanstack/react-query";
import { ChevronLeft, ChevronRight, SlidersHorizontal } from "lucide-react";
import { useState } from "react";
import { SchoolCard } from "@/components/school-card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { searchSchools, type SchoolFilters } from "@/lib/api";
import { schoolTypes, schoolTypeLabels } from "@/lib/types";
import type { PageResult, SchoolListItem, SchoolType } from "@/lib/types";

const initial: SchoolFilters = { page: 1, pageSize: 9, sortBy: "name", sortDirection: "asc" };

export default function SchoolsBrowser({
  initialFilters,
  initialData,
}: {
  initialFilters: SchoolFilters;
  initialData: PageResult<SchoolListItem>;
}) {
  const [draft, setDraft] = useState<SchoolFilters>(initialFilters);
  const [filters, setFilters] = useState<SchoolFilters>(initialFilters);

  const schools = useQuery({
    queryKey: ["schools", filters],
    queryFn: () => searchSchools(filters),
    initialData,
    placeholderData: (previous) => previous,
  });

  function update<K extends keyof SchoolFilters>(key: K, value: SchoolFilters[K]) {
    setDraft((current) => ({ ...current, [key]: value }));
  }

  function applyFilters(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFilters({ ...draft, page: 1 });
  }

  function clearFilters() {
    setDraft(initial);
    setFilters(initial);
  }

  const pageCount = Math.max(1, Math.ceil((schools.data?.totalCount ?? 0) / (filters.pageSize ?? 9)));

  return (
    <div className="container-shell py-10 sm:py-14">
      <div className="max-w-3xl"><p className="eyebrow">School directory</p><h1 className="mt-3 text-4xl font-bold tracking-tight text-slate-950">Explore schools in Greece</h1><p className="mt-3 text-lg leading-7 text-slate-600">Find schools by location, learning stage, and the practical details that help you decide.</p></div>
      <div className="mt-9 grid gap-8 lg:grid-cols-[250px_1fr]">
        <aside>
          <form onSubmit={applyFilters} className="filter-panel">
            <div className="flex items-center justify-between"><h2 className="flex items-center gap-2 font-bold text-slate-900"><SlidersHorizontal size={17} /> Filters</h2><button type="button" onClick={clearFilters} className="text-xs font-semibold text-teal-700 hover:underline">Clear all</button></div>
            <div className="mt-6 space-y-5">
              <div className="space-y-2"><Label htmlFor="keyword">School or area</Label><Input id="keyword" value={draft.keyword ?? ""} onChange={(event) => update("keyword", event.target.value)} placeholder="Try Athens" /></div>
              <div className="space-y-2"><Label htmlFor="city">City</Label><Input id="city" value={draft.city ?? ""} onChange={(event) => update("city", event.target.value)} placeholder="e.g. Athens" /></div>
              <div className="space-y-2"><Label htmlFor="type">School type</Label><select id="type" className="form-select" value={draft.type ?? ""} onChange={(event) => update("type", event.target.value as SchoolType | "")}><option value="">All school types</option>{schoolTypes.map((type) => <option key={type} value={type}>{schoolTypeLabels[type]}</option>)}</select></div>
              <div className="space-y-2"><Label htmlFor="schoolKind">School ownership</Label><select id="schoolKind" className="form-select" value={draft.isPrivate == null ? "" : String(draft.isPrivate)} onChange={(event) => update("isPrivate", event.target.value === "" ? null : event.target.value === "true")}><option value="">Public and private</option><option value="false">Public</option><option value="true">Private</option></select></div>
              <div><Label>Annual tuition (€)</Label><div className="mt-2 grid grid-cols-2 gap-2"><Input type="number" min="0" placeholder="Min" value={draft.minTuition ?? ""} onChange={(event) => update("minTuition", event.target.value === "" ? null : Number(event.target.value))} aria-label="Minimum tuition" /><Input type="number" min="0" placeholder="Max" value={draft.maxTuition ?? ""} onChange={(event) => update("maxTuition", event.target.value === "" ? null : Number(event.target.value))} aria-label="Maximum tuition" /></div></div>
              <Button type="submit" className="w-full">Show schools</Button>
            </div>
          </form>
        </aside>
        <section>
          <div className="mb-5 flex flex-wrap items-center justify-between gap-3"><p className="text-sm text-slate-500">{schools.data ? <><b className="text-slate-800">{schools.data.totalCount}</b> schools found</> : "Loading schools…"}</p><select className="form-select w-auto min-w-44" aria-label="Sort schools" value={`${filters.sortBy}:${filters.sortDirection}`} onChange={(event) => { const [sortBy, sortDirection] = event.target.value.split(":"); setFilters((current) => ({ ...current, sortBy, sortDirection: sortDirection as "asc" | "desc", page: 1 })); }}><option value="name:asc">Name A–Z</option><option value="name:desc">Name Z–A</option><option value="city:asc">City A–Z</option><option value="tuitionFrom:asc">Tuition: low to high</option></select></div>
          {schools.isError && <p role="alert" className="rounded-xl bg-rose-50 p-4 text-rose-800">We could not load school results. Check that the API is available and try again.</p>}
          {schools.isLoading ? <div className="grid gap-5 sm:grid-cols-2 xl:grid-cols-3">{Array.from({ length: 6 }, (_, index) => <div key={index} className="skeleton-card" />)}</div> : null}
          {schools.data && schools.data.items.length > 0 ? <div className="grid gap-5 sm:grid-cols-2 xl:grid-cols-3">{schools.data.items.map((school) => <SchoolCard key={school.id} school={school} />)}</div> : null}
          {schools.data && schools.data.items.length === 0 ? <div className="empty-state"><div className="empty-state-icon"><SlidersHorizontal /></div><h2>No schools match those filters</h2><p>Try changing your search or clearing one of the filters.</p><Button variant="outline" onClick={clearFilters}>Clear filters</Button></div> : null}
          <div className="mt-8 flex items-center justify-between border-t border-slate-200 pt-5"><p className="text-sm text-slate-500">Page {filters.page} of {pageCount}</p><div className="flex gap-2"><Button variant="outline" size="sm" disabled={(filters.page ?? 1) <= 1 || schools.isFetching} onClick={() => setFilters((current) => ({ ...current, page: (current.page ?? 1) - 1 }))}><ChevronLeft size={16} /> Previous</Button><Button variant="outline" size="sm" disabled={(filters.page ?? 1) >= pageCount || schools.isFetching} onClick={() => setFilters((current) => ({ ...current, page: (current.page ?? 1) + 1 }))}>Next <ChevronRight size={16} /></Button></div></div>
        </section>
      </div>
    </div>
  );
}
