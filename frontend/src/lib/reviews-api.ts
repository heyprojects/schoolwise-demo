import type { QueryClient } from "@tanstack/react-query";
import { api } from "@/lib/api";
import type { PageResult } from "@/lib/types";

export type ReviewStatus = "PendingModeration" | "Published" | "Rejected";
export interface ReviewContent {
  overallRating: number;
  teachingRating: number;
  safetyRating: number;
  communicationRating: number;
  facilitiesRating: number;
  activitiesRating: number;
  pros: string | null;
  cons: string | null;
  reviewText: string;
}
export interface PublicReview {
  id: string;
  content: ReviewContent;
  publishedAt: string;
}
export interface ManagedReview {
  id: string;
  schoolId: string;
  schoolName: string;
  schoolSlug: string;
  version: number;
  status: ReviewStatus;
  content: ReviewContent;
  updatedAt: string;
  moderationNotes: string | null;
}
export interface ReviewHistory {
  version: number;
  status: ReviewStatus;
  content: ReviewContent;
  submittedAt: string;
  reviewedAt: string | null;
  moderationNotes: string | null;
  withdrawnAt: string | null;
}
export interface SchoolRatings {
  reviewCount: number;
  overallScore: number | null;
  teachingScore: number | null;
  safetyScore: number | null;
  communicationScore: number | null;
  facilitiesScore: number | null;
  activitiesScore: number | null;
  distribution: { rating: number; count: number }[];
}

export const ratingFields = [
  ["overallRating", "Overall", "overallScore"],
  ["teachingRating", "Teaching", "teachingScore"],
  ["safetyRating", "Safety", "safetyScore"],
  ["communicationRating", "Communication", "communicationScore"],
  ["facilitiesRating", "Facilities", "facilitiesScore"],
  ["activitiesRating", "Activities", "activitiesScore"],
] as const;

export const reviewStatusLabels: Record<ReviewStatus, string> = {
  PendingModeration: "Pending moderation",
  Published: "Published",
  Rejected: "Rejected",
};

export async function fetchSchoolReviews(schoolId: string, page: number, rating: number | null, sort: string) {
  const { data } = await api.get<PageResult<PublicReview>>(`/schools/${schoolId}/reviews`, {
    params: { page, pageSize: 10, rating: rating ?? undefined, sort },
  });
  return data;
}
export async function fetchSchoolRatings(schoolId: string) {
  const { data } = await api.get<SchoolRatings>(`/schools/${schoolId}/ratings`);
  return data;
}
export async function fetchMyReviews(page = 1, status?: ReviewStatus, schoolId?: string) {
  const { data } = await api.get<PageResult<ManagedReview>>("/reviews/mine", { params: { page, status, schoolId } });
  return data;
}
export async function fetchModerationReviews(page: number, status: ReviewStatus) {
  const { data } = await api.get<PageResult<ManagedReview>>("/reviews/moderation", { params: { page, status } });
  return data;
}
export async function fetchReviewHistory(id: string, page: number) {
  const { data } = await api.get<PageResult<ReviewHistory>>(`/reviews/${id}/history`, { params: { page } });
  return data;
}
export async function saveReview(schoolId: string, content: ReviewContent, existing?: ManagedReview) {
  const { data } = existing
    ? await api.put<ManagedReview>(`/reviews/${existing.id}`, { version: existing.version, content })
    : await api.post<ManagedReview>("/reviews", { schoolId, content });
  return data;
}
export async function deleteReview(review: ManagedReview) {
  await api.delete(`/reviews/${review.id}`, { params: { version: review.version } });
}
export async function moderateReview(review: ManagedReview, approve: boolean, notes: string) {
  await api.post(`/reviews/${review.id}/moderation`, { version: review.version, approve, notes: notes.trim() || null });
}
export async function invalidateReviews(client: QueryClient, schoolId: string) {
  await Promise.all([
    client.invalidateQueries({ queryKey: ["my-reviews"] }),
    client.invalidateQueries({ queryKey: ["moderation-reviews"] }),
    client.invalidateQueries({ queryKey: ["review-history"] }),
    client.invalidateQueries({ queryKey: ["school-reviews", schoolId] }),
    client.invalidateQueries({ queryKey: ["school-ratings", schoolId] }),
  ]);
}
