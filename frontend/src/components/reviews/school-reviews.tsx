"use client";

import { useLanguage } from "@/components/providers/language-provider";

import Link from "next/link";
import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useAuth } from "@/components/providers/auth-provider";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { ReviewBody } from "@/components/reviews/review-body";
import { ReviewForm } from "@/components/reviews/review-form";
import { getErrorMessage } from "@/lib/api";
import { fetchMyReviews, fetchSchoolRatings, fetchSchoolReviews, ratingFields, reviewStatusLabels } from "@/lib/reviews-api";

export function SchoolReviews({ schoolId }: { schoolId: string }) {
  const { t, message, date, number } = useLanguage();
  const { user, loading } = useAuth();
  const [page, setPage] = useState(1);
  const [rating, setRating] = useState<number | null>(null);
  const [sort, setSort] = useState("newest");
  const [feedback, setMessage] = useState("");
  const summary = useQuery({ queryKey: ["school-ratings", schoolId], queryFn: () => fetchSchoolRatings(schoolId) });
  const reviews = useQuery({ queryKey: ["school-reviews", schoolId, page, rating, sort], queryFn: () => fetchSchoolReviews(schoolId, page, rating, sort) });
  const mine = useQuery({
    queryKey: ["my-reviews", user?.id, schoolId],
    queryFn: () => fetchMyReviews(1, undefined, schoolId), enabled: user?.role === "Parent",
  });
  const current = mine.data?.items[0];
  return <section id="reviews" className="detail-section mt-8 space-y-6">
    <div><p className="eyebrow">{t("Parent experiences")}</p><h2>{t("Ratings and reviews")}</h2><p className="text-sm text-slate-500">{t("Only published reviews count. School relationships are not verified in this release; moderation is not a guarantee of factual accuracy.")}</p></div>
    {summary.isPending && <p>{t("Loading ratings...")}</p>}
    {summary.error && <p role="alert">{message(getErrorMessage(summary.error))}</p>}
    {summary.data && <div className="grid gap-5 rounded-xl bg-slate-50 p-5 sm:grid-cols-2">
      <div><p className="text-3xl font-bold">{summary.data.overallScore == null ? t("Not rated yet") : `${number(summary.data.overallScore, { minimumFractionDigits: 1, maximumFractionDigits: 1 })} / 5`}</p><p>{t(summary.data.reviewCount === 1 ? "{count} published review" : "{count} published reviews", { count: number(summary.data.reviewCount) })}</p>
        <dl className="mt-4 grid grid-cols-2 gap-3">{ratingFields.slice(1).map(([, label, score]) => {
          const value = summary.data?.[score];
          return <div key={score}><dt className="text-sm text-slate-500">{t(label)}</dt><dd className="font-semibold">{value == null ? "-" : number(value, { minimumFractionDigits: 1, maximumFractionDigits: 1 })} / 5</dd></div>;
        })}</dl>
      </div>
      <div aria-label={t("Rating distribution")}>{summary.data.distribution.slice().reverse().map(bucket => <div key={bucket.rating} className="mb-2 flex items-center gap-3 text-sm"><span>{bucket.rating} / 5</span><meter aria-label={t("{rating} star reviews", { rating: bucket.rating })} min={0} max={Math.max(1, summary.data?.reviewCount ?? 0)} value={bucket.count} className="h-4 flex-1" /><span>{number(bucket.count)}</span></div>)}</div>
    </div>}
    <div className="flex flex-wrap gap-4">
      <div><Label htmlFor="review-rating-filter">{t("Filter by overall rating")}</Label><select id="review-rating-filter" className="mt-2 block rounded-md border p-2" value={rating ?? ""} onChange={(event) => { setRating(event.target.value ? Number(event.target.value) : null); setPage(1); }}><option value="">{t("All ratings")}</option>{[5, 4, 3, 2, 1].map(value => <option key={value} value={value}>{value} / 5</option>)}</select></div>
      <div><Label htmlFor="review-sort">{t("Sort reviews")}</Label><select id="review-sort" className="mt-2 block rounded-md border p-2" value={sort} onChange={(event) => { setSort(event.target.value); setPage(1); }}><option value="newest">{t("Newest first")}</option><option value="highest">{t("Highest rating")}</option><option value="lowest">{t("Lowest rating")}</option></select></div>
    </div>
    {reviews.isPending && <p>{t("Loading reviews...")}</p>}
    {reviews.error && <p role="alert">{message(getErrorMessage(reviews.error))}</p>}
    {reviews.data?.items.length === 0 && <p>{t("No published reviews match these filters.")}</p>}
    {reviews.data?.items.map(review => <article key={review.id} className="space-y-4 rounded-xl border p-5"><div><h3 className="font-semibold">{t("Parent review")}</h3><p className="text-sm text-slate-500">{t("Published {date}", { date: date(review.publishedAt) })}</p></div><ReviewBody content={review.content} /></article>)}
    <div className="flex flex-wrap items-center gap-4"><Button type="button" variant="outline" disabled={page <= 1 || reviews.isFetching} onClick={() => setPage(page - 1)}>{t("Previous reviews")}</Button><span>{t("Page {page}", { page: number(page) })}</span><Button type="button" variant="outline" disabled={!reviews.data || page * reviews.data.pageSize >= reviews.data.totalCount || reviews.isFetching} onClick={() => setPage(page + 1)}>{t("Next reviews")}</Button></div>
    {feedback && <p role="status" className="rounded-lg bg-teal-50 p-3 text-teal-800">{message(feedback)}</p>}
    {loading ? <p>{t("Checking your account...")}</p> : !user ? <p><Link href="/login" className="nav-link">{t("Log in")}</Link>{" "}{t("or")}{" "}<Link href="/register" className="nav-link">{t("register")}</Link>{" "}{t("to write a review.")}</p> : user.role === "Parent" ? <>
      {mine.isPending && <p>{t("Loading your review...")}</p>}
      {mine.error && <p role="alert">{message(getErrorMessage(mine.error))}</p>}
      {current && <div className="space-y-2"><p>{t("Your review:")}{" "}<strong>{t(reviewStatusLabels[current.status])}</strong></p>{current.moderationNotes && <p>{t("Admin note:")}{" "}{current.moderationNotes}</p>}<Link href="/my-reviews" className="nav-link">{t("My Reviews - history and deletion")}</Link></div>}
      {mine.isSuccess && <ReviewForm key={`${current?.id ?? "new"}-${current?.version ?? 0}`} schoolId={schoolId} existing={current} onSaved={(review) => setMessage(review.status === "Draft" ? "Your private draft was saved. Only you can see it until you submit it." : "Your review was submitted and is waiting for moderation.")} />}
    </> : <p>{t("Only Parent accounts can write reviews. Administrators manage the moderation queue.")}</p>}
  </section>;
}
