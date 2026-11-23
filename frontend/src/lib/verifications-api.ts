import { api } from "@/lib/api";
import type { PageResult } from "@/lib/types";

export interface IssuedCode {
  id: string;
  code: string;
}

export interface VerificationCodeStatus {
  id: string;
  createdAt: string;
  expiresAt: string;
  validUntil: string;
  redeemedAt: string | null;
  revokedAt: string | null;
}

export interface ParentVerification {
  id: string;
  schoolId: string;
  schoolName: string;
  verifiedAt: string;
  validUntil: string;
  status: "Active" | "Expired" | "Revoked";
}

export async function issueVerificationCodes(input: {
  schoolId: string;
  count: number;
  expiresAt: string;
  validUntil: string;
}) {
  const { data } = await api.post<IssuedCode[]>("/verifications/codes", input);
  return data;
}

export async function listVerificationCodes(schoolId: string, page: number) {
  const { data } = await api.get<PageResult<VerificationCodeStatus>>("/verifications/codes", { params: { schoolId, page } });
  return data;
}

export async function revokeVerificationCode(id: string, reason: string) {
  await api.post(`/verifications/codes/${id}/revoke`, { reason });
}

export async function redeemVerificationCode(schoolId: string, code: string) {
  await api.post("/verifications/redeem", { schoolId, code });
}

export async function listOwnVerifications(page: number) {
  const { data } = await api.get<PageResult<ParentVerification>>("/verifications/me", { params: { page } });
  return data;
}
