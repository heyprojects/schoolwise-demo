import type { Metadata } from "next";
import Image from "next/image";
import Link from "next/link";
import { notFound } from "next/navigation";
import { ArrowLeft, Globe2, Mail, MapPin, Phone, School as SchoolIcon } from "lucide-react";
import { fetchServerSchoolBySlug } from "@/lib/server-api";
import { schoolTypeLabels } from "@/lib/types";
import { SchoolReviews } from "@/components/reviews/school-reviews";

export const dynamic = "force-dynamic";

type SchoolDetailsPageProps = {
  params: Promise<{ slug: string }>;
};

export async function generateMetadata({ params }: SchoolDetailsPageProps): Promise<Metadata> {
  const { slug } = await params;
  const school = await fetchServerSchoolBySlug(slug);
  if (!school) {
    return { title: "School not found | SchoolWise" };
  }

  return {
    title: `${school.name} | SchoolWise`,
    description: school.description?.slice(0, 160) ?? `${school.name} in ${school.city}, Greece.`,
    alternates: { canonical: `/schools/${encodeURIComponent(school.slug)}` },
  };
}

export default async function SchoolDetailsPage({ params }: SchoolDetailsPageProps) {
  const { slug } = await params;
  const school = await fetchServerSchoolBySlug(slug);
  if (!school) {
    notFound();
  }

  const tuition = school.isPrivate
    ? school.tuitionFrom == null
      ? "Contact school for tuition"
      : `€${school.tuitionFrom.toLocaleString("en-GB")}${school.tuitionTo != null ? ` – €${school.tuitionTo.toLocaleString("en-GB")}` : "+"} per year`
    : "Public school";

  return (
    <div className="container-shell py-8 sm:py-12">
      <Link href="/schools" className="back-link"><ArrowLeft size={16} /> Back to schools</Link>
      <section className="detail-hero mt-6">
        <div className="flex flex-col gap-6 sm:flex-row sm:items-center">
          <div className="detail-logo">{school.logoUrl ? <Image src={school.logoUrl} alt="" width={160} height={160} unoptimized className="h-full w-full rounded-3xl object-cover" /> : <SchoolIcon size={40} />}</div>
          <div className="min-w-0 flex-1"><div className="flex flex-wrap gap-2"><span className="detail-tag">{schoolTypeLabels[school.schoolType]}</span><span className="detail-tag">{school.isPrivate ? "Private" : "Public"}</span></div><h1 className="mt-3 text-3xl font-bold tracking-tight text-slate-950 sm:text-4xl">{school.name}</h1><p className="mt-2 flex items-center gap-1.5 text-slate-600"><MapPin size={17} /> {school.address}, {school.city}{school.postalCode ? ` ${school.postalCode}` : ""}</p></div>
          <div className="tuition-box"><span>Annual tuition</span><b>{tuition}</b></div>
        </div>
      </section>
      <div className="mt-8 grid gap-8 lg:grid-cols-[1fr_320px]">
        <div className="space-y-8">
          <section className="detail-section"><p className="eyebrow">Overview</p><h2>About this school</h2><p className="whitespace-pre-line leading-7 text-slate-600">{school.description || "More information about this school will be added soon."}</p>{school.establishedYear && <p className="mt-4 text-sm text-slate-500">Established {school.establishedYear}</p>}</section>
          <section className="detail-section"><p className="eyebrow">Facilities</p><h2>What to know</h2><p className="leading-7 text-slate-600">Facilities information is not included in this listing yet. Contact the school to confirm current facilities and services.</p></section>
          <section className="detail-section"><p className="eyebrow">Gallery</p><h2>Photos</h2>{school.photos.length ? <div className="mt-5 grid gap-3 sm:grid-cols-2">{school.photos.map((photo) => <Image key={photo.id} src={photo.url} alt={`${school.name} photo`} width={640} height={400} unoptimized className="h-56 w-full rounded-2xl bg-slate-100 object-cover" />)}</div> : <div className="photo-placeholder"><SchoolIcon size={35} /><span>Photos have not been added yet.</span></div>}</section>
        </div>
        <aside><div className="contact-card"><p className="eyebrow">Get in touch</p><h2>Contact information</h2><div className="mt-5 space-y-4">{school.phone && <a className="contact-line" href={`tel:${school.phone}`}><Phone size={17} />{school.phone}</a>}{school.email && <a className="contact-line" href={`mailto:${school.email}`}><Mail size={17} />{school.email}</a>}{school.website && <a className="contact-line" href={school.website} target="_blank" rel="noreferrer"><Globe2 size={17} />Visit website</a>}{!school.phone && !school.email && !school.website && <p className="text-sm leading-6 text-slate-500">Contact details have not been provided for this listing.</p>}</div></div></aside>
      </div>
      <SchoolReviews schoolId={school.id} />
    </div>
  );
}
