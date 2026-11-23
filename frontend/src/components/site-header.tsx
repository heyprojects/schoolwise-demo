"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { LogOut, School } from "lucide-react";
import { useAuth } from "@/components/providers/auth-provider";
import { Button, buttonVariants } from "@/components/ui/button";

export function SiteHeader() {
  const { user, loading, logout } = useAuth();
  const router = useRouter();

  async function handleLogout() {
    await logout();
    router.push("/");
  }

  return (
    <header className="site-header">
      <div className="container-shell flex h-[76px] items-center justify-between gap-6">
        <Link href="/" className="flex items-center gap-2.5 font-bold tracking-tight text-slate-950">
          <span className="brand-mark"><School size={21} strokeWidth={2.2} /></span>
          <span className="text-xl">SchoolWise</span>
        </Link>
        <nav className="hidden items-center gap-8 text-sm font-medium text-slate-600 sm:flex">
          <Link className="nav-link" href="/schools">Explore schools</Link>
          <Link className="nav-link" href="/#how-it-works">How it works</Link>
        </nav>
        <div className="flex items-center gap-2">
          {loading ? (
            <span className="text-sm text-slate-500">Loading…</span>
          ) : user ? (
            <>
              {user.role === "Admin" && <Link href="/admin/schools" className="mr-2 hidden text-sm font-semibold text-slate-700 sm:inline">Admin</Link>}
              <span className="hidden text-sm text-slate-600 md:inline">Hi, {user.firstName}</span>
              <Button variant="outline" size="sm" onClick={handleLogout} aria-label="Sign out">
                <LogOut size={15} /> <span className="hidden sm:inline">Sign out</span>
              </Button>
            </>
          ) : (
            <>
              <Link href="/login" className="hidden px-3 py-2 text-sm font-semibold text-slate-700 sm:inline">Log in</Link>
              <Link className={buttonVariants({ size: "sm" })} href="/register">Create account</Link>
            </>
          )}
        </div>
      </div>
    </header>
  );
}
