import HomeClient from "./home-client";
import { fetchServerSchools } from "@/lib/server-api";

export const dynamic = "force-dynamic";

export default async function HomePage() {
  const params = new URLSearchParams({ page: "1", pageSize: "3", sortBy: "name", sortDirection: "asc" });
  const initialSchools = await fetchServerSchools(params);
  return <HomeClient initialSchools={initialSchools} />;
}
