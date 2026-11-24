"use client";

import { useLanguage } from "@/components/providers/language-provider";

import Link from "next/link";
import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useAuth } from "@/components/providers/auth-provider";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { ReviewBody } from "@/components/reviews/review-body";
import { ReviewHistory } from "@/components/reviews/review-history";
import { getErrorMessage } from "@/lib/api";
import { fetchModerationReviews, invalidateReviews, moderateReview, reviewStatusLabels, type ManagedReview, type ModerationReviewStatus } from "@/lib/reviews-api";

function ModerationCard({ review }: { review: ManagedReview }) {
  const { t, message } = useLanguage();
  const client = useQueryClient();
  const [notes, setNotes] = useState("");
  const [history, setHistory] = useState(false);
  const decision = useMutation({
    mutationFn: (approve: boolean) => moderateReview(review, approve, notes),
    onSuccess: () => invalidateReviews(client, review.schoolId),
  });
  return <article className="space-y-4 rounded-xl border bg-white p-5">
    <div><h2 className="text-xl font-semibold"><Link href={`/schools/${encodeURIComponent(review.schoolSlug)}`}>{review.schoolName}</Link></h2><p>{t("{status} / version {version}", { status: t(reviewStatusLabels[review.status]), version: review.version })}</p></div>
    <ReviewBody content={review.content} />
    {review.moderationNotes && <p>{t("Previous note:")}{" "}{review.moderationNotes}</p>}
    <Button type="button" variant="outline" onClick={() => setHistory(!history)}>{t("Version history")}</Button>
    {history && <ReviewHistory id={review.id} />}
    {review.status === "PendingModeration" && <div className="space-y-3">
      <p className="text-sm text-slate-600">{t("Check for identifying child/staff details, sensitive information and unsupported accusations before publishing.")}</p>
      <Label htmlFor={`notes-${review.id}`}>{t("Moderation note / rejection reason (visible to the parent)")}</Label>
      <textarea id={`notes-${review.id}`} maxLength={1000} className="min-h-24 w-full rounded-md border p-3" value={notes} onChange={(event) => setNotes(event.target.value)} />
      {decision.error && <p role="alert" className="rounded-lg bg-rose-50 p-3 text-rose-800">{message(getErrorMessage(decision.error))}{" "}{t("Reload the queue if the version changed.")}</p>}
      <div className="flex gap-3"><Button type="button" disabled={decision.isPending} onClick={() => decision.mutate(true)}>{t("Approve")}</Button><Button type="button" variant="destructive" disabled={decision.isPending || !notes.trim()} onClick={() => decision.mutate(false)}>{t("Reject")}</Button></div>
    </div>}
  </article>;
}

export default function AdminReviewsPage() {
  const { t, message } = useLanguage();
  const { user, loading } = useAuth();
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState<ModerationReviewStatus>("PendingModeration");
  const query = useQuery({ queryKey: ["moderation-reviews", user?.id, status, page], queryFn: () => fetchModerationReviews(page, status), enabled: user?.role === "Admin" });
  if (loading) return <div className="container-shell py-16">{t("Checking administrator access...")}</div>;
  if (user?.role !== "Admin") return <div className="container-shell py-16"><h1>{t("Administrator access required")}</h1><Link href="/login">{t("Log in")}</Link></div>;
  return <div className="container-shell space-y-6 py-10">
    <h1 className="text-3xl font-bold">{t("Review moderation")}</h1>
    <p>{t("Approve only the version displayed. Notes are visible to the author, never to visitors. Unsubmitted drafts are private and excluded from this queue and its version history.")}</p>
    <Link href="/admin/schools" className="nav-link">{t("Manage schools")}</Link>
    <div><Label htmlFor="moderation-status">{t("Status")}</Label><select id="moderation-status" className="ml-3 rounded-md border p-2" value={status} onChange={(event) => {
      const value = event.target.value; if (value === "PendingModeration" || value === "Published" || value === "Rejected") { setStatus(value); setPage(1); }
    }}>{Object.entries(reviewStatusLabels).filter(([value]) => value !== "Draft").map(([value, label]) => <option key={value} value={value}>{t(label)}</option>)}</select><Button type="button" variant="outline" className="ml-3" onClick={() => void query.refetch()}>{t("Reload queue")}</Button></div>
    {query.isPending && <p>{t("Loading reviews...")}</p>}
    {query.error && <p role="alert">{message(getErrorMessage(query.error))}</p>}
    {query.data?.items.length === 0 && <p>{t("No reviews in this queue.")}</p>}
    {query.data?.items.map(review => <ModerationCard key={`${review.id}-${review.version}-${review.status}`} review={review} />)}
    <div className="flex items-center gap-4"><Button type="button" variant="outline" disabled={page <= 1 || query.isFetching} onClick={() => setPage(page - 1)}>{t("Previous")}</Button><span>{t("Page {page}", { page })}</span><Button type="button" variant="outline" disabled={!query.data || page * query.data.pageSize >= query.data.totalCount || query.isFetching} onClick={() => setPage(page + 1)}>{t("Next")}</Button></div>
  </div>;
}
