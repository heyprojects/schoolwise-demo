import axios, { type InternalAxiosRequestConfig } from "axios";
import type {
  AuthSession,
  PageResult,
  School,
  SchoolListItem,
  SchoolType,
  SchoolWriteInput,
  User,
} from "@/lib/types";

export const api = axios.create({
  baseURL: (process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:8080/api/v1").replace(/\/$/, ""),
  withCredentials: true,
  headers: { "Content-Type": "application/json" },
});

let accessToken: string | null = null;
let refreshInFlight: Promise<AuthSession> | null = null;

export function setAccessToken(token: string | null) {
  accessToken = token;
}

api.interceptors.request.use((config) => {
  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`;
  }
  return config;
});

api.interceptors.response.use(undefined, async (error: unknown) => {
  if (!axios.isAxiosError(error)) {
    return Promise.reject(error);
  }

  const request = error.config as (InternalAxiosRequestConfig & { _retry?: boolean }) | undefined;
  const isAuthEndpoint = request?.url?.includes("/auth/login") || request?.url?.includes("/auth/refresh");
  if (error.response?.status !== 401 || !request || request._retry || isAuthEndpoint) {
    return Promise.reject(error);
  }

  request._retry = true;
  try {
    const session = await refreshSession();
    request.headers.Authorization = `Bearer ${session.accessToken}`;
    return await api.request(request);
  } catch {
    setAccessToken(null);
    return Promise.reject(error);
  }
});

export interface SchoolFilters {
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: "asc" | "desc";
  keyword?: string;
  city?: string;
  type?: SchoolType | "";
  isPrivate?: boolean | null;
  minTuition?: number | null;
  maxTuition?: number | null;
}

export async function searchSchools(filters: SchoolFilters = {}) {
  const { data } = await api.get<PageResult<SchoolListItem>>("/schools", {
    params: Object.fromEntries(
      Object.entries(filters).filter(([, value]) => value !== "" && value !== null && value !== undefined),
    ),
  });
  return data;
}

export async function getAdminSchool(id: string) {
  const { data } = await api.get<School>(`/schools/admin/${id}`);
  return data;
}

export async function searchAdminSchools(page = 1) {
  const { data } = await api.get<PageResult<SchoolListItem>>("/schools/admin", {
    params: { page, pageSize: 50, sortBy: "name" },
  });
  return data;
}

export async function createSchool(input: SchoolWriteInput) {
  const { data } = await api.post<School>("/schools", input);
  return data;
}

export async function updateSchool(id: string, input: SchoolWriteInput) {
  const { data } = await api.put<School>(`/schools/${id}`, input);
  return data;
}

export async function deleteSchool(id: string) {
  await api.delete(`/schools/${id}`);
}

export async function login(email: string, password: string) {
  const { data } = await api.post<AuthSession>("/auth/login", { email, password });
  setAccessToken(data.accessToken);
  return data.user;
}

export async function register(input: {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}) {
  const { data } = await api.post<User>("/auth/register", input);
  return data;
}

export async function refreshSession() {
  if (!refreshInFlight) {
    refreshInFlight = api.post<AuthSession>("/auth/refresh").then(({ data }) => {
      setAccessToken(data.accessToken);
      return data;
    }).finally(() => {
      refreshInFlight = null;
    });
  }
  return refreshInFlight;
}

export async function currentUser() {
  const { data } = await api.get<User>("/auth/me");
  return data;
}

export async function logout() {
  try {
    await api.post("/auth/revoke");
  } finally {
    setAccessToken(null);
  }
}

export function getErrorMessage(error: unknown) {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as { detail?: string; title?: string; errors?: Record<string, string[]> } | undefined;
    const firstValidationError = data?.errors ? Object.values(data.errors).flat()[0] : undefined;
    return firstValidationError ?? data?.detail ?? data?.title ?? error.message;
  }
  return error instanceof Error ? error.message : "Something went wrong. Please try again.";
}
