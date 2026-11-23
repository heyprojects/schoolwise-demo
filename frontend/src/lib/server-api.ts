import type { PageResult, School, SchoolListItem } from "@/lib/types";

const apiBaseUrl = (
  process.env.INTERNAL_API_URL ??
  process.env.NEXT_PUBLIC_API_URL ??
  "http://localhost:8080/api/v1"
).replace(/\/$/, "");

export async function fetchServerSchools(params: URLSearchParams) {
  const response = await fetch(`${apiBaseUrl}/schools?${params.toString()}`, {
    next: { revalidate: 60 },
  });
  if (!response.ok) {
    throw new Error(`School search failed with status ${response.status}.`);
  }
  return (await response.json()) as PageResult<SchoolListItem>;
}

export async function fetchServerSchoolBySlug(slug: string) {
  // Next route params may already be percent-encoded.
  const encodedSlug = encodeURIComponent(decodeURIComponent(slug));
  const response = await fetch(`${apiBaseUrl}/schools/slug/${encodedSlug}`, {
    next: { revalidate: 300 },
  });
  if (response.status === 404) {
    return null;
  }
  if (!response.ok) {
    throw new Error(`School detail request failed with status ${response.status}.`);
  }
  return (await response.json()) as School;
}
