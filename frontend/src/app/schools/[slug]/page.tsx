import type { Metadata } from "next";
import Image from "next/image";
import Link from "next/link";
import { notFound } from "next/navigation";
import { ArrowLeft, Globe2, Mail, MapPin, Phone, School as SchoolIcon } from "lucide-react";
import { fetchServerSchoolBySlug } from "@/lib/server-api";
import { schoolTypeLabels } from "@/lib/types";
import { SchoolReviews } from "@/components/reviews/school-reviews";
import { getServerLanguage } from "@/lib/server-language";
import { formatTuition } from "@/lib/i18n";

export const dynamic = "force-dynamic";

type SchoolDetailsPageProps = {
  params: Promise<{ slug: string }>;
};

export async function generateMetadata({ params }: SchoolDetailsPageProps): Promise<Metadata> {
  const { t } = await getServerLanguage();
  const { slug } = await params;
  const school = await fetchServerSchoolBySlug(slug);
  if (!school) {
    return { title: t("School not found | SchoolWise") };
  }

  return {
    title: `${school.name} | SchoolWise`,
    description: school.description?.slice(0, 160) ?? t("{name} in {city}, Greece.", { name: school.name, city: school.city }),
    alternates: { canonical: `/schools/${encodeURIComponent(school.slug)}` },
  };
}

export default async function SchoolDetailsPage({ params }: SchoolDetailsPageProps) {
  const { locale, t } = await getServerLanguage();
  const { slug } = await params;
  const school = await fetchServerSchoolBySlug(slug);
  if (!school) {
    notFound();
  }

  const tuition = formatTuition(locale, school, true);

  return (
    <div className="container-shell py-8 sm:py-12">
      <Link href="/schools" className="back-link"><ArrowLeft size={16} />{" "}{t("Back to schools")}</Link>
      <section className="detail-hero mt-6">
        <div className="flex flex-col gap-6 sm:flex-row sm:items-center">
          <div className="detail-logo">{school.logoUrl ? <Image src={school.logoUrl} alt="" width={160} height={160} unoptimized className="h-full w-full rounded-3xl object-cover" /> : <SchoolIcon size={40} />}</div>
          <div className="min-w-0 flex-1"><div className="flex flex-wrap gap-2"><span className="detail-tag">{t(schoolTypeLabels[school.schoolType])}</span><span className="detail-tag">{school.isPrivate ? t("Private") : t("Public")}</span></div><h1 className="editorial-title mt-3 text-3xl sm:text-4xl">{school.name}</h1><p className="mt-2 flex items-center gap-1.5 text-slate-600"><MapPin size={17} className="shrink-0" /> {school.address}, {school.city}{school.postalCode ? ` ${school.postalCode}` : ""}</p></div>
          <div className="tuition-box"><span>{t("Annual tuition")}</span><b>{tuition}</b></div>
        </div>
      </section>
      <div className="mt-8 grid gap-8 lg:grid-cols-[1fr_320px]">
        <div className="space-y-8">
          <section className="detail-section"><p className="eyebrow">{t("Overview")}</p><h2>{t("About this school")}</h2><p className="whitespace-pre-line leading-7 text-slate-600">{school.description || t("More information about this school will be added soon.")}</p>{school.establishedYear && <p className="mt-4 text-sm text-slate-500">{t("Established {year}", { year: school.establishedYear })}</p>}</section>
          <section className="detail-section"><p className="eyebrow">{t("Facilities")}</p><h2>{t("What to know")}</h2><p className="leading-7 text-slate-600">{t("Facilities information is not included in this listing yet. Contact the school to confirm current facilities and services.")}</p></section>
          <section className="detail-section"><p className="eyebrow">{t("Gallery")}</p><h2>{t("Photos")}</h2>{school.photos.length ? <div className="mt-5 grid gap-3 sm:grid-cols-2">{school.photos.map((photo) => <Image key={photo.id} src={photo.url} alt={t("{name} photo", { name: school.name })} width={640} height={400} unoptimized className="h-56 w-full rounded-2xl bg-slate-100 object-cover" />)}</div> : <div className="photo-placeholder"><SchoolIcon size={35} /><span>{t("Photos have not been added yet.")}</span></div>}</section>
        </div>
        <aside><div className="contact-card"><p className="eyebrow">{t("Get in touch")}</p><h2>{t("Contact information")}</h2><div className="mt-5 space-y-4">{school.phone && <a className="contact-line" href={`tel:${school.phone}`}><Phone size={17} />{school.phone}</a>}{school.email && <a className="contact-line" href={`mailto:${school.email}`}><Mail size={17} />{school.email}</a>}{school.website && <a className="contact-line" href={school.website} target="_blank" rel="noreferrer"><Globe2 size={17} />{t("Visit website")}</a>}{!school.phone && !school.email && !school.website && <p className="text-sm leading-6 text-slate-500">{t("Contact details have not been provided for this listing.")}</p>}</div></div></aside>
      </div>
      <SchoolReviews schoolId={school.id} />
    </div>
  );
}
