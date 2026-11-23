"use client";

import Link from "next/link";
import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useAuth } from "@/components/providers/auth-provider";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { ReviewBody } from "@/components/reviews/review-body";
import { ReviewHistory } from "@/components/reviews/review-history";
import { getErrorMessage } from "@/lib/api";
import { fetchModerationReviews, invalidateReviews, moderateReview, reviewStatusLabels, type ManagedReview, type ReviewStatus } from "@/lib/reviews-api";

function ModerationCard({ review }: { review: ManagedReview }) {
  const client = useQueryClient();
  const [notes, setNotes] = useState("");
  const [history, setHistory] = useState(false);
  const decision = useMutation({
    mutationFn: (approve: boolean) => moderateReview(review, approve, notes),
    onSuccess: () => invalidateReviews(client, review.schoolId),
  });
  return <article className="space-y-4 rounded-xl border bg-white p-5">
    <div><h2 className="text-xl font-semibold"><Link href={`/schools/${encodeURIComponent(review.schoolSlug)}`}>{review.schoolName}</Link></h2><p>{reviewStatusLabels[review.status]} / version {review.version}</p></div>
    <ReviewBody content={review.content} />
    {review.moderationNotes && <p>Previous note: {review.moderationNotes}</p>}
    <Button type="button" variant="outline" onClick={() => setHistory(!history)}>Version history</Button>
    {history && <ReviewHistory id={review.id} />}
    {review.status === "PendingModeration" && <div className="space-y-3">
      <p className="text-sm text-slate-600">Check for identifying child/staff details, sensitive information and unsupported accusations before publishing.</p>
      <Label htmlFor={`notes-${review.id}`}>Moderation note / rejection reason (visible to the parent)</Label>
      <textarea id={`notes-${review.id}`} maxLength={1000} className="min-h-24 w-full rounded-md border p-3" value={notes} onChange={(event) => setNotes(event.target.value)} />
      {decision.error && <p role="alert" className="rounded-lg bg-rose-50 p-3 text-rose-800">{getErrorMessage(decision.error)} Reload the queue if the version changed.</p>}
      <div className="flex gap-3"><Button type="button" disabled={decision.isPending} onClick={() => decision.mutate(true)}>Approve</Button><Button type="button" variant="destructive" disabled={decision.isPending || !notes.trim()} onClick={() => decision.mutate(false)}>Reject</Button></div>
    </div>}
  </article>;
}

export default function AdminReviewsPage() {
  const { user, loading } = useAuth();
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState<ReviewStatus>("PendingModeration");
  const query = useQuery({ queryKey: ["moderation-reviews", user?.id, status, page], queryFn: () => fetchModerationReviews(page, status), enabled: user?.role === "Admin" });
  if (loading) return <div className="container-shell py-16">Checking administrator access...</div>;
  if (user?.role !== "Admin") return <div className="container-shell py-16"><h1>Administrator access required</h1><Link href="/login">Log in</Link></div>;
  return <div className="container-shell space-y-6 py-10">
    <h1 className="text-3xl font-bold">Review moderation</h1>
    <p>Approve only the version displayed. Notes are visible to the author, never to visitors.</p>
    <Link href="/admin/schools" className="nav-link">Manage schools</Link>
    <div><Label htmlFor="moderation-status">Status</Label><select id="moderation-status" className="ml-3 rounded-md border p-2" value={status} onChange={(event) => {
      const value = event.target.value; if (value === "PendingModeration" || value === "Published" || value === "Rejected") { setStatus(value); setPage(1); }
    }}>{Object.entries(reviewStatusLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select><Button type="button" variant="outline" className="ml-3" onClick={() => void query.refetch()}>Reload queue</Button></div>
    {query.isPending && <p>Loading reviews...</p>}
    {query.error && <p role="alert">{getErrorMessage(query.error)}</p>}
    {query.data?.items.length === 0 && <p>No reviews in this queue.</p>}
    {query.data?.items.map(review => <ModerationCard key={`${review.id}-${review.version}-${review.status}`} review={review} />)}
    <div className="flex items-center gap-4"><Button type="button" variant="outline" disabled={page <= 1 || query.isFetching} onClick={() => setPage(page - 1)}>Previous</Button><span>Page {page}</span><Button type="button" variant="outline" disabled={!query.data || page * query.data.pageSize >= query.data.totalCount || query.isFetching} onClick={() => setPage(page + 1)}>Next</Button></div>
  </div>;
}
