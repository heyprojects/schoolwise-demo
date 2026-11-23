import Link from "next/link";
import Image from "next/image";
import { ArrowUpRight, MapPin, School as SchoolIcon } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import type { SchoolListItem } from "@/lib/types";
import { schoolTypeLabels } from "@/lib/types";

export function SchoolCard({ school }: { school: SchoolListItem }) {
  const tuition = school.isPrivate
    ? school.tuitionFrom == null
      ? "Tuition not listed"
      : `€${school.tuitionFrom.toLocaleString("en-GB")}${school.tuitionTo != null ? `–€${school.tuitionTo.toLocaleString("en-GB")}` : "+"} / year`
    : "Public school";

  return (
    <Link href={`/schools/${school.slug}`} className="school-card group">
      <div className="flex items-start justify-between gap-3">
        <div className="school-logo">
          {school.logoUrl ? (
            // External image hosts are school-managed; use a native image to avoid unsafe host allowlists.
            <Image src={school.logoUrl} alt="" width={120} height={120} unoptimized className="h-full w-full rounded-2xl object-cover" />
          ) : (
            <SchoolIcon size={28} strokeWidth={1.6} />
          )}
        </div>
        <Badge variant="secondary" className="rounded-full bg-slate-100 px-3 py-1 text-slate-600">
          {school.isPrivate ? "Private" : "Public"}
        </Badge>
      </div>
      <h3 className="mt-5 line-clamp-2 text-lg font-bold tracking-tight text-slate-900 group-hover:text-teal-700">{school.name}</h3>
      <div className="mt-3 flex items-center gap-1.5 text-sm text-slate-500">
        <MapPin size={15} /> {school.city}
      </div>
      <div className="mt-5 flex items-center justify-between border-t border-slate-100 pt-4">
        <div>
          <p className="text-xs font-medium uppercase tracking-wide text-slate-400">{schoolTypeLabels[school.schoolType]}</p>
          <p className="mt-1 text-sm font-semibold text-slate-700">{tuition}</p>
        </div>
        <span className="rounded-full bg-teal-50 p-2 text-teal-700 transition group-hover:bg-teal-700 group-hover:text-white">
          <ArrowUpRight size={17} />
        </span>
      </div>
    </Link>
  );
}
