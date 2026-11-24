"use client";

import { useLanguage } from "@/components/providers/language-provider";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useAuth } from "@/components/providers/auth-provider";
import { Button } from "@/components/ui/button";
import { ReviewBody } from "@/components/reviews/review-body";
import { getErrorMessage } from "@/lib/api";
import { fetchReviewHistory, reviewStatusLabels } from "@/lib/reviews-api";

export function ReviewHistory({ id }: { id: string }) {
  const { t, message, dateTime } = useLanguage();
  const { user } = useAuth();
  const [page, setPage] = useState(1);
  const query = useQuery({ queryKey: ["review-history", user?.id, id, page], queryFn: () => fetchReviewHistory(id, page), enabled: Boolean(user) });
  return <section className="mt-4 space-y-4 rounded-lg bg-slate-50 p-4">
    <h3 className="font-semibold">{t("Version history")}</h3>
    {query.isPending && <p>{t("Loading history...")}</p>}
    {query.error && <p role="alert">{message(getErrorMessage(query.error))}</p>}
    {query.data?.items.map(version => <article key={version.version} className="space-y-3 rounded-lg border bg-white p-4">
      <h4 className="font-semibold">{t("Version {version} - {status}", { version: version.version, status: t(reviewStatusLabels[version.status]) })}</h4>
      <p className="text-sm text-slate-500">{version.status === "Draft" ? t("Saved") : t("Submitted")} {dateTime(version.submittedAt)}{version.withdrawnAt ? ` / ${t("withdrawn or superseded")}` : ""}</p>
      <ReviewBody content={version.content} />
      {version.moderationNotes && <p className="text-sm">{t("Admin note:")}{" "}{version.moderationNotes}</p>}
    </article>)}
    <div className="flex gap-3"><Button type="button" variant="outline" disabled={page <= 1 || query.isFetching} onClick={() => setPage(page - 1)}>{t("Previous versions")}</Button><Button type="button" variant="outline" disabled={!query.data || page * query.data.pageSize >= query.data.totalCount || query.isFetching} onClick={() => setPage(page + 1)}>{t("Older versions")}</Button></div>
  </section>;
}
