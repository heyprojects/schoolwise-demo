"use client";

import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { getErrorMessage } from "@/lib/api";
import { invalidateReviews, ratingFields, saveReview, type ManagedReview, type ReviewContent } from "@/lib/reviews-api";

const rating = z.number().int().min(1, "Choose a rating for every category.").max(5);
const schema = z.object({
  overallRating: rating, teachingRating: rating, safetyRating: rating,
  communicationRating: rating, facilitiesRating: rating, activitiesRating: rating,
  pros: z.string().max(1000).nullable(), cons: z.string().max(1000).nullable(),
  reviewText: z.string().trim().min(20, "Write at least 20 characters about your experience.").max(5000),
});
const blank: ReviewContent = {
  overallRating: 0, teachingRating: 0, safetyRating: 0, communicationRating: 0, facilitiesRating: 0,
  activitiesRating: 0, pros: "", cons: "", reviewText: "",
};

export function ReviewForm({ schoolId, existing, onSaved }: {
  schoolId: string; existing?: ManagedReview; onSaved?: () => void;
}) {
  const client = useQueryClient();
  const [content, setContent] = useState<ReviewContent>(() => existing?.content ?? blank);
  const [validation, setValidation] = useState("");
  const save = useMutation({
    mutationFn: (input: ReviewContent) => saveReview(schoolId, input, existing),
    onSuccess: async () => { await invalidateReviews(client, schoolId); onSaved?.(); },
  });
  function submit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const parsed = schema.safeParse(content);
    if (!parsed.success) { setValidation(parsed.error.issues[0]?.message ?? "Check the review fields."); return; }
    setValidation(""); save.mutate(parsed.data);
  }
  return <form onSubmit={submit} className="space-y-5 rounded-xl border border-slate-200 p-5">
    <h3 className="text-xl font-semibold">{existing ? "Edit your review" : "Write a review"}</h3>
    <p className="text-sm text-slate-600">Share your own experience. Do not include child names, identifying details about staff, contact information or sensitive personal data. Every submission is moderated.</p>
    {existing && <p className="rounded-lg bg-amber-50 p-3 text-sm">Editing temporarily removes the review and its rating until an admin approves the new version. Earlier versions remain in your history.</p>}
    <div className="grid gap-4 sm:grid-cols-2">{ratingFields.map(([field, label]) => <div key={field}>
      <Label htmlFor={`review-${field}`}>{label} rating</Label>
      <select id={`review-${field}`} required className="mt-2 w-full rounded-md border p-2" value={content[field] || ""}
        onChange={(event) => setContent({ ...content, [field]: Number(event.target.value) })}>
        <option value="">Select 1-5</option>{[1, 2, 3, 4, 5].map(value => <option key={value} value={value}>{value} / 5</option>)}
      </select>
    </div>)}</div>
    <div><Label htmlFor="review-pros">Pros (optional)</Label><textarea id="review-pros" maxLength={1000} className="mt-2 min-h-20 w-full rounded-md border p-3" value={content.pros ?? ""} onChange={(event) => setContent({ ...content, pros: event.target.value })} /></div>
    <div><Label htmlFor="review-cons">Cons (optional)</Label><textarea id="review-cons" maxLength={1000} className="mt-2 min-h-20 w-full rounded-md border p-3" value={content.cons ?? ""} onChange={(event) => setContent({ ...content, cons: event.target.value })} /></div>
    <div><Label htmlFor="review-text">Your experience</Label><textarea id="review-text" required minLength={20} maxLength={5000} className="mt-2 min-h-36 w-full rounded-md border p-3" value={content.reviewText} onChange={(event) => setContent({ ...content, reviewText: event.target.value })} /><p className="text-xs text-slate-500">20-5,000 characters. Publication is subject to approval.</p></div>
    {(validation || save.error) && <p role="alert" className="rounded-lg bg-rose-50 p-3 text-rose-800">{validation || getErrorMessage(save.error)}</p>}
    <Button type="submit" disabled={save.isPending}>{save.isPending ? "Submitting..." : existing ? "Submit updated review" : "Submit review"}</Button>
  </form>;
}
