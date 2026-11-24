"use client";

import { useLanguage } from "@/components/providers/language-provider";

import Link from "next/link";
import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useAuth } from "@/components/providers/auth-provider";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { getErrorMessage, searchSchools } from "@/lib/api";
import { listOwnVerifications, redeemVerificationCode } from "@/lib/verifications-api";

export default function VerificationsPage() {
  const { t, message, date } = useLanguage();
  const { user, loading } = useAuth();
  const client = useQueryClient();
  const [schoolSearch, setSchoolSearch] = useState("");
  const [schoolId, setSchoolId] = useState("");
  const [code, setCode] = useState("");
  const [page, setPage] = useState(1);
  const allowed = user?.role === "Parent";
  const schools = useQuery({
    queryKey: ["verification-schools", schoolSearch],
    queryFn: () => searchSchools({ keyword: schoolSearch, pageSize: 50 }),
    enabled: allowed,
  });
  const verifications = useQuery({
    queryKey: ["own-verifications", user?.id, page],
    queryFn: () => listOwnVerifications(page),
    enabled: allowed,
  });
  const redeem = useMutation({
    mutationFn: ({ schoolId, code }: { schoolId: string; code: string }) => redeemVerificationCode(schoolId, code),
    onSuccess: async () => {
      setCode(""); setPage(1);
      await client.invalidateQueries({ queryKey: ["own-verifications"] });
    },
  });
  if (loading) return <div className="container-shell py-16">{t("Checking your account...")}</div>;
  if (!allowed) return <div className="container-shell py-16"><h1>{t("A Parent account is required")}</h1><p>{t("Administrators cannot redeem parent codes.")}</p><Link href="/login">{t("Log in")}</Link> / <Link href="/register">{t("Create a Parent account")}</Link></div>;
  const error = redeem.error ?? schools.error ?? verifications.error;

  function submit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    redeem.mutate({ schoolId, code: code.trim() });
  }

  return (
    <div className="container-shell space-y-6 py-10">
      <div><p className="eyebrow">{t("Parent account")}</p><h1 className="mt-3 text-3xl font-bold">{t("School verification")}</h1></div>
      <p>{t("Enter a single-use code supplied privately by your participating school. Never upload documents or provide your child's details here.")}</p>
      <p className="text-sm text-slate-500">{t("Demo foundation: email confirmation is not available. Reviews are submitted independently of codes and moderated before publication. Code access alone does not establish identity or guarantee a review is true.")}</p>
      {error && <p role="alert" className="rounded-lg bg-rose-50 p-3 text-rose-800">{message(getErrorMessage(error))}</p>}
      {redeem.isSuccess && <p role="status" className="rounded-lg bg-teal-50 p-3 text-teal-800">{t("Code redeemed. Your school eligibility is recorded below.")}</p>}
      <form onSubmit={submit} className="max-w-xl space-y-4 rounded-xl border p-5">
        <div><Label htmlFor="school-search">{t("Search schools")}</Label><Input id="school-search" value={schoolSearch} onChange={(event) => setSchoolSearch(event.target.value)} /></div>
        <div><Label htmlFor="school">{t("School")}</Label><select id="school" required className="mt-2 w-full rounded-md border p-2" value={schoolId} onChange={(event) => { setSchoolId(event.target.value); redeem.reset(); }}><option value="">{t("Select the school that issued your code")}</option>{schools.data?.items.map((school) => <option key={school.id} value={school.id}>{school.name} ({school.city})</option>)}</select></div>
        <div><Label htmlFor="code">{t("School-issued code")}</Label><Input id="code" required autoComplete="off" spellCheck={false} maxLength={100} value={code} onChange={(event) => { setCode(event.target.value); redeem.reset(); }} /></div>
        <Button type="submit" disabled={!schoolId || !code.trim() || redeem.isPending}>{redeem.isPending ? t("Checking...") : t("Redeem code")}</Button>
      </form>
      <section className="space-y-4"><h2 className="text-xl font-semibold">{t("My school eligibility")}</h2>
        {verifications.isPending && <p>{t("Loading your eligibility...")}</p>}
        {verifications.data?.items.length === 0 && <p>{t("You have not redeemed a school code yet.")}</p>}
        <ul className="space-y-3">{verifications.data?.items.map((verification) => <li key={verification.id} className="rounded-lg border p-4"><h3 className="font-semibold">{verification.schoolName}</h3><p>{t(verification.status)}</p><p className="text-sm text-slate-500">{t("Valid until {date} (UTC)", { date: date(verification.validUntil, { timeZone: "UTC" }) })}</p></li>)}</ul>
        <div className="flex items-center gap-4"><Button type="button" variant="outline" disabled={page === 1 || verifications.isFetching} onClick={() => setPage(page - 1)}>{t("Previous")}</Button><span>{t("Page {page}", { page })}</span><Button type="button" variant="outline" disabled={!verifications.data || page * verifications.data.pageSize >= verifications.data.totalCount || verifications.isFetching} onClick={() => setPage(page + 1)}>{t("Next")}</Button></div>
      </section>
    </div>
  );
}
