import SchoolsBrowser from "./schools-browser";
import { fetchServerSchools } from "@/lib/server-api";
import type { SchoolFilters } from "@/lib/api";

export const dynamic = "force-dynamic";

type SchoolsPageProps = {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
};

function firstValue(value: string | string[] | undefined) {
  return Array.isArray(value) ? value[0] ?? "" : value ?? "";
}

export default async function SchoolsPage({ searchParams }: SchoolsPageProps) {
  const params = await searchParams;
  const initialFilters: SchoolFilters = {
    page: 1,
    pageSize: 9,
    sortBy: "name",
    sortDirection: "asc",
    keyword: firstValue(params.keyword),
    city: firstValue(params.city),
  };
  const query = new URLSearchParams({
    page: String(initialFilters.page),
    pageSize: String(initialFilters.pageSize),
    sortBy: initialFilters.sortBy ?? "name",
    sortDirection: initialFilters.sortDirection ?? "asc",
  });
  if (initialFilters.keyword) query.set("keyword", initialFilters.keyword);
  if (initialFilters.city) query.set("city", initialFilters.city);

  const initialData = await fetchServerSchools(query);
  return <SchoolsBrowser initialFilters={initialFilters} initialData={initialData} />;
}
