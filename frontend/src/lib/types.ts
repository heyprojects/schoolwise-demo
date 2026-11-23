export type SchoolType =
  | "Nursery"
  | "Kindergarten"
  | "Elementary"
  | "MiddleSchool"
  | "HighSchool";

export type UserRole = "Parent" | "SchoolAdmin" | "Admin";

export interface SchoolPhoto {
  id: string;
  url: string;
  sortOrder: number;
}

export interface SchoolListItem {
  id: string;
  name: string;
  slug: string;
  schoolType: SchoolType;
  address: string;
  city: string;
  tuitionFrom: number | null;
  tuitionTo: number | null;
  isPrivate: boolean;
  isApproved: boolean;
  logoUrl: string | null;
}

export interface School extends SchoolListItem {
  description: string | null;
  postalCode: string | null;
  phone: string | null;
  email: string | null;
  website: string | null;
  studentCapacity: number | null;
  establishedYear: number | null;
  createdAt: string;
  photos: SchoolPhoto[];
}

export interface SchoolWriteInput {
  name: string;
  description: string;
  schoolType: SchoolType;
  address: string;
  city: string;
  postalCode: string;
  phone: string;
  email: string;
  website: string;
  tuitionFrom: number | null;
  tuitionTo: number | null;
  studentCapacity: number | null;
  establishedYear: number | null;
  isPrivate: boolean;
  logoUrl: string;
  isApproved: boolean;
  photos: { url: string; sortOrder: number }[];
}

export interface PageResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface User {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  role: UserRole;
  isActive: boolean;
  createdAt: string;
}

export interface AuthSession {
  accessToken: string;
  expiresAt: string;
  user: User;
}

export const schoolTypes: SchoolType[] = [
  "Nursery",
  "Kindergarten",
  "Elementary",
  "MiddleSchool",
  "HighSchool",
];

export const schoolTypeLabels: Record<SchoolType, string> = {
  Nursery: "Nursery",
  Kindergarten: "Kindergarten",
  Elementary: "Elementary",
  MiddleSchool: "Middle school",
  HighSchool: "High school",
};
