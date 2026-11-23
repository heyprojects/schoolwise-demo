"use client";

import Link from "next/link";
import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useAuth } from "@/components/providers/auth-provider";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { getErrorMessage, searchSchools } from "@/lib/api";
import { issueVerificationCodes, listVerificationCodes, revokeVerificationCode, type IssuedCode } from "@/lib/verifications-api";

function futureDate(days: number) {
  return new Date(Date.now() + days * 86400000).toISOString().slice(0, 10);
}

function schoolYearEnd() {
  const now = new Date();
  return `${now.getUTCMonth() >= 8 ? now.getUTCFullYear() + 1 : now.getUTCFullYear()}-08-31`;
}

export default function AdminVerificationsPage() {
  const { user, loading } = useAuth();
  const client = useQueryClient();
  const [schoolSearch, setSchoolSearch] = useState("");
  const [schoolId, setSchoolId] = useState("");
  const [count, setCount] = useState(10);
  const [expiresAt, setExpiresAt] = useState(() => futureDate(7));
  const [validUntil, setValidUntil] = useState(schoolYearEnd);
  const [page, setPage] = useState(1);
  const [reason, setReason] = useState("");
  const [issued, setIssued] = useState<{ userId: string; schoolId: string; codes: IssuedCode[] } | null>(null);
  const allowed = user?.role === "Admin";
  const schools = useQuery({
    queryKey: ["verification-schools", schoolSearch],
    queryFn: () => searchSchools({ keyword: schoolSearch, pageSize: 50 }),
    enabled: allowed,
  });
  const codes = useQuery({
    queryKey: ["verification-codes", user?.id, schoolId, page],
    queryFn: () => listVerificationCodes(schoolId, page),
    enabled: allowed && Boolean(schoolId),
  });
  const issue = useMutation({
    mutationFn: issueVerificationCodes,
    onSuccess: async (data, input) => {
      if (user) setIssued({ userId: user.id, schoolId: input.schoolId, codes: data });
      setPage(1);
      await client.invalidateQueries({ queryKey: ["verification-codes"] });
    },
  });
  const revoke = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) => revokeVerificationCode(id, reason),
    onSuccess: () => client.invalidateQueries({ queryKey: ["verification-codes"] }),
  });

  if (loading) return <div className="container-shell py-16">Checking administrator access...</div>;
  if (!allowed) return <div className="container-shell py-16"><h1>Administrator access required</h1><Link href="/login">Log in</Link></div>;
  const error = issue.error ?? revoke.error ?? schools.error ?? codes.error;
  const shownCodes = issued?.userId === user.id && issued.schoolId === schoolId ? issued.codes : [];

  function submit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIssued(null);
    issue.mutate({ schoolId, count, expiresAt: `${expiresAt}T23:59:59Z`, validUntil: `${validUntil}T23:59:59Z` });
  }

  return (
    <div className="container-shell space-y-6 py-10">
      <div><p className="eyebrow">Administration</p><h1 className="mt-3 text-3xl font-bold">School verification codes</h1></div>
      <p>Demo foundation only. Issue codes only for an agreed partner-school pilot. A code is not an identity check or a guarantee that a review is true.</p>
      <Link href="/admin/schools" className="nav-link">Manage schools</Link>
      {error && <p role="alert" className="rounded-lg bg-rose-50 p-3 text-rose-800">{getErrorMessage(error)}</p>}
      <form onSubmit={submit} className="max-w-xl space-y-4 rounded-xl border p-5">
        <div><Label htmlFor="school-search">Search approved schools</Label><Input id="school-search" value={schoolSearch} onChange={(event) => setSchoolSearch(event.target.value)} /></div>
        <div><Label htmlFor="school">School</Label><select id="school" required className="mt-2 w-full rounded-md border p-2" value={schoolId} onChange={(event) => {
          setSchoolId(event.target.value); setPage(1); setIssued(null); issue.reset(); revoke.reset();
        }}><option value="">Select a school</option>{schools.data?.items.map((school) => <option key={school.id} value={school.id}>{school.name} ({school.city})</option>)}</select></div>
        <div><Label htmlFor="count">Number of single-use codes (1-50)</Label><Input id="count" type="number" min={1} max={50} required value={count} onChange={(event) => setCount(event.target.valueAsNumber)} /></div>
        <div><Label htmlFor="expires">Redeem before (UTC, within 30 days)</Label><Input id="expires" type="date" required value={expiresAt} onChange={(event) => setExpiresAt(event.target.value)} /></div>
        <div><Label htmlFor="valid-until">Parent eligibility valid until (UTC)</Label><Input id="valid-until" type="date" required value={validUntil} onChange={(event) => setValidUntil(event.target.value)} /><p className="text-sm text-slate-500">Use the agreed school-year cutoff, never more than 12 months. Eligibility must not end before code expiry.</p></div>
        <Button type="submit" disabled={!schoolId || issue.isPending}>{issue.isPending ? "Issuing..." : "Issue codes"}</Button>
      </form>
      {shownCodes.length > 0 && <section className="space-y-3 rounded-xl border border-amber-300 p-5">
        <h2 className="text-xl font-semibold">Copy now: codes are shown only once</h2>
        <p>Store and distribute securely through the school. Do not paste these codes in chat, logs or public pages. Leaving this screen loses the plaintext codes.</p>
        <textarea aria-label="Issued verification codes" readOnly className="h-48 w-full rounded border p-3 font-mono text-sm" value={shownCodes.map((code) => `${code.id}\t${code.code}`).join("\n")} />
        <Button type="button" variant="outline" onClick={() => setIssued(null)}>Hide codes</Button>
      </section>}
      {schoolId && <section className="space-y-4">
        <h2 className="text-xl font-semibold">Code history</h2>
        <p>Revoking a redeemed code also ends the eligibility granted by that code.</p>
        <div className="max-w-xl"><Label htmlFor="reason">Reason for revocation (required)</Label><Input id="reason" maxLength={500} value={reason} onChange={(event) => setReason(event.target.value)} /></div>
        {codes.isPending && <p>Loading code history...</p>}
        {codes.data?.items.length === 0 && <p>No codes issued yet.</p>}
        <ul className="space-y-3">{codes.data?.items.map((code) => <li key={code.id} className="flex flex-wrap items-center justify-between gap-3 rounded-lg border p-4">
          <div><p className="break-all font-mono text-sm">{code.id}</p><p>{code.revokedAt ? "Revoked" : code.redeemedAt ? "Redeemed" : new Date(code.expiresAt).getTime() <= Date.now() ? "Expired" : "Available"}</p><p className="text-sm text-slate-500">Redeem before {new Date(code.expiresAt).toLocaleDateString(undefined, { timeZone: "UTC" })} / eligibility until {new Date(code.validUntil).toLocaleDateString(undefined, { timeZone: "UTC" })} (UTC)</p></div>
          <Button type="button" variant="outline" disabled={Boolean(code.revokedAt) || !reason.trim() || revoke.isPending} onClick={() => {
            if (window.confirm("Revoke this code and any eligibility it granted?")) revoke.mutate({ id: code.id, reason: reason.trim() });
          }}>Revoke</Button>
        </li>)}</ul>
        <div className="flex items-center gap-4"><Button type="button" variant="outline" disabled={page === 1 || codes.isFetching} onClick={() => setPage(page - 1)}>Previous</Button><span>Page {page}</span><Button type="button" variant="outline" disabled={!codes.data || page * codes.data.pageSize >= codes.data.totalCount || codes.isFetching} onClick={() => setPage(page + 1)}>Next</Button></div>
      </section>}
    </div>
  );
}
