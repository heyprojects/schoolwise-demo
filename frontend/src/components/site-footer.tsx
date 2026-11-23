import Link from "next/link";

export function SiteFooter() {
  return (
    <footer className="mt-24 border-t border-slate-200 bg-white">
      <div className="container-shell flex flex-col gap-5 py-8 text-sm text-slate-500 sm:flex-row sm:items-center sm:justify-between">
        <p>© {new Date().getFullYear()} SchoolWise. Helping families make confident choices.</p>
        <div className="flex gap-5">
          <Link href="/schools" className="hover:text-slate-900">Explore schools</Link>
          <Link href="/login" className="hover:text-slate-900">School admin</Link>
        </div>
      </div>
    </footer>
  );
}
