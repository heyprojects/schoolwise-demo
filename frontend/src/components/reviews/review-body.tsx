import { ratingFields, type ReviewContent } from "@/lib/reviews-api";

export function ReviewBody({ content }: { content: ReviewContent }) {
  return <div className="space-y-4">
    <dl className="grid grid-cols-2 gap-2 text-sm sm:grid-cols-3">{ratingFields.map(([field, label]) =>
      <div key={field}><dt className="text-slate-500">{label}</dt><dd className="font-semibold">{content[field] === 0 ? "Not selected" : `${content[field]} / 5`}</dd></div>)}</dl>
    {content.pros && <div><h4 className="font-semibold">Pros</h4><p className="whitespace-pre-line break-words text-slate-600">{content.pros}</p></div>}
    {content.cons && <div><h4 className="font-semibold">Cons</h4><p className="whitespace-pre-line break-words text-slate-600">{content.cons}</p></div>}
    <p className="whitespace-pre-line break-words leading-7">{content.reviewText || "No review text yet."}</p>
  </div>;
}
