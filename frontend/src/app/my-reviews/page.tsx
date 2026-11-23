"use client";

import Link from "next/link";
import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useAuth } from "@/components/providers/auth-provider";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { ReviewBody } from "@/components/reviews/review-body";
import { ReviewForm } from "@/components/reviews/review-form";
import { ReviewHistory } from "@/components/reviews/review-history";
import { getErrorMessage } from "@/lib/api";
import { deleteReview, fetchMyReviews, invalidateReviews, reviewStatusLabels, type ReviewStatus } from "@/lib/reviews-api";

export default function MyReviewsPage() {
  const { user, loading } = useAuth();
  const client = useQueryClient();
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState<ReviewStatus | undefined>();
  const [editing, setEditing] = useState<string | null>(null);
  const [history, setHistory] = useState<string | null>(null);
  const query = useQuery({ queryKey: ["my-reviews", user?.id, page, status], queryFn: () => fetchMyReviews(page, status), enabled: user?.role === "Parent" });
  const remove = useMutation({ mutationFn: deleteReview, onSuccess: (_, review) => invalidateReviews(client, review.schoolId) });
  if (loading) return <div className="container-shell py-16">Checking your account...</div>;
  if (user?.role !== "Parent") return <div className="container-shell py-16"><h1>A Parent account is required</h1><Link href="/login">Log in</Link></div>;
  return <div className="container-shell space-y-6 py-10">
    <h1 className="text-3xl font-bold">My Reviews</h1>
    <p>Save private drafts and track your submissions. Submitted reviews and edits require approval before publication.</p>
    <Link href="/schools" className="nav-link">Find a school to review</Link>
    <div><Label htmlFor="my-review-status">Review status</Label><select id="my-review-status" className="ml-3 rounded-md border p-2" value={status ?? ""} onChange={(event) => {
      const value = event.target.value;
      setStatus(value === "Draft" || value === "PendingModeration" || value === "Published" || value === "Rejected" ? value : undefined); setPage(1);
    }}><option value="">All statuses</option>{Object.entries(reviewStatusLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select><Button type="button" variant="outline" className="ml-3" onClick={() => void query.refetch()}>Reload</Button></div>
    {query.isPending && <p>Loading your reviews...</p>}
    {(query.error || remove.error) && <p role="alert" className="rounded-lg bg-rose-50 p-3 text-rose-800">{getErrorMessage(query.error ?? remove.error)}</p>}
    {query.data?.items.length === 0 && <p>No reviews match this status.</p>}
    {query.data?.items.map(review => <article key={review.id} className="space-y-4 rounded-xl border bg-white p-5">
      <div><h2 className="text-xl font-semibold"><Link href={`/schools/${encodeURIComponent(review.schoolSlug)}`}>{review.schoolName}</Link></h2><p>{reviewStatusLabels[review.status]} / version {review.version}</p></div>
      {review.moderationNotes && <p className="rounded-lg bg-slate-50 p-3">Admin note: {review.moderationNotes}</p>}
      <ReviewBody content={review.content} />
      <div className="flex flex-wrap gap-3"><Button type="button" variant="outline" onClick={() => setEditing(editing === review.id ? null : review.id)}>{review.status === "Draft" ? "Continue draft" : "Edit"}</Button><Button type="button" variant="outline" onClick={() => setHistory(history === review.id ? null : review.id)}>Version history</Button><Button type="button" variant="destructive" disabled={remove.isPending} onClick={() => {
        if (window.confirm("Delete your review? It will immediately disappear from public reviews and ratings. Version history is retained.")) remove.mutate(review);
      }}>Delete</Button></div>
      {editing === review.id && <ReviewForm key={`${review.id}-${review.version}`} schoolId={review.schoolId} existing={review} onSaved={() => setEditing(null)} />}
      {history === review.id && <ReviewHistory id={review.id} />}
    </article>)}
    <div className="flex items-center gap-4"><Button type="button" variant="outline" disabled={page <= 1 || query.isFetching} onClick={() => setPage(page - 1)}>Previous</Button><span>Page {page}</span><Button type="button" variant="outline" disabled={!query.data || page * query.data.pageSize >= query.data.totalCount || query.isFetching} onClick={() => setPage(page + 1)}>Next</Button></div>
  </div>;
}
