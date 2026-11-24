"use client";

import { useLanguage } from "@/components/providers/language-provider";
import Link from "next/link";
import Image from "next/image";
import { ArrowUpRight, MapPin, School as SchoolIcon } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import type { SchoolListItem } from "@/lib/types";
import { schoolTypeLabels } from "@/lib/types";
import { formatTuition } from "@/lib/i18n";

export function SchoolCard({ school }: { school: SchoolListItem }) {
  const { t, locale } = useLanguage();
  const tuition = formatTuition(locale, school);

  return (
    <Link href={`/schools/${school.slug}`} className="school-card group" data-school-type={school.schoolType}>
      <div className="school-card-art">
        <div className="school-logo">
          {school.logoUrl ? (
            // External image hosts are school-managed; use a native image to avoid unsafe host allowlists.
            <Image src={school.logoUrl} alt="" width={120} height={120} unoptimized className="h-full w-full object-contain" />
          ) : (
            <SchoolIcon size={64} strokeWidth={0.9} aria-hidden="true" />
          )}
        </div>
        <Badge variant="secondary" className="school-ownership">
          {school.isPrivate ? t("Private") : t("Public")}
        </Badge>
      </div>
      <div className="school-card-body">
        <p className="school-stage">{t(schoolTypeLabels[school.schoolType])}</p>
        <h3 className="mt-3 line-clamp-2 text-lg font-semibold leading-7 tracking-tight text-slate-900 group-hover:text-primary">{school.name}</h3>
        <div className="school-location mt-3 flex items-center gap-1.5 text-sm text-slate-500">
          <MapPin size={15} className="shrink-0" /> {school.city}
        </div>
        <div className="school-card-bottom">
          <p className="text-sm font-medium text-slate-700">{tuition}</p>
          <span className="school-card-arrow"><ArrowUpRight size={17} /></span>
        </div>
      </div>
    </Link>
  );
}
