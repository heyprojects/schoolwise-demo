"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { ChevronLeft, ChevronRight, Pencil, Plus, Trash2 } from "lucide-react";
import { useAuth } from "@/components/providers/auth-provider";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { createSchool, deleteSchool, getAdminSchool, getErrorMessage, searchAdminSchools, updateSchool } from "@/lib/api";
import { schoolTypes, schoolTypeLabels, type School, type SchoolListItem, type SchoolType, type SchoolWriteInput } from "@/lib/types";

const emptyForm: SchoolWriteInput = {
  name: "", description: "", schoolType: "Kindergarten", address: "", city: "Athens", postalCode: "",
  phone: "", email: "", website: "", tuitionFrom: null, tuitionTo: null, studentCapacity: null,
  establishedYear: null, isPrivate: true, logoUrl: "", isApproved: false, photos: [],
};

function toInput(school: School): SchoolWriteInput {
  return {
    name: school.name, description: school.description ?? "", schoolType: school.schoolType,
    address: school.address, city: school.city, postalCode: school.postalCode ?? "",
    phone: school.phone ?? "", email: school.email ?? "", website: school.website ?? "",
    tuitionFrom: school.tuitionFrom, tuitionTo: school.tuitionTo, studentCapacity: school.studentCapacity,
    establishedYear: school.establishedYear, isPrivate: school.isPrivate, logoUrl: school.logoUrl ?? "",
    isApproved: school.isApproved, photos: school.photos.map((photo) => ({ url: photo.url, sortOrder: photo.sortOrder })),
  };
}

export default function AdminSchoolsPage() {
  const { user, loading } = useAuth();
  const router = useRouter();
  const queryClient = useQueryClient();
  const [page, setPage] = useState(1);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<SchoolWriteInput>(emptyForm);
  const [editorOpen, setEditorOpen] = useState(false);
  const [error, setError] = useState("");
  const query = useQuery({ queryKey: ["admin-schools", page], queryFn: () => searchAdminSchools(page) });
  const pageCount = Math.max(1, Math.ceil((query.data?.totalCount ?? 0) / (query.data?.pageSize ?? 50)));

  useEffect(() => {
    if (!loading && !user) router.replace("/login");
  }, [loading, user, router]);

  const save = useMutation({
    mutationFn: () => editingId ? updateSchool(editingId, form) : createSchool(form),
    onSuccess: async () => {
      if (!editingId) setPage(1);
      await queryClient.invalidateQueries({ queryKey: ["admin-schools"] });
      setEditingId(null); setForm(emptyForm); setEditorOpen(false); setError("");
    },
    onError: (reason) => setError(getErrorMessage(reason)),
  });
  const remove = useMutation({
    mutationFn: deleteSchool,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["admin-schools"] }),
    onError: (reason) => setError(getErrorMessage(reason)),
  });

  if (loading || !user) return <div className="container-shell py-16 text-slate-500">Checking administrator access…</div>;
  if (user.role !== "Admin") return <div className="container-shell py-16"><div className="empty-state"><h1>Administrator access required</h1><p>Your account cannot manage school listings.</p></div></div>;

  function beginEdit(school: SchoolListItem) {
    setEditingId(school.id); setForm({ ...emptyForm }); setEditorOpen(true); setError("");
    void queryClient.fetchQuery({ queryKey: ["admin-school", school.id], queryFn: () => getAdminSchool(school.id) })
      .then((fullSchool) => { setForm(toInput(fullSchool)); })
      .catch((reason: unknown) => setError(getErrorMessage(reason)));
  }

  function change<K extends keyof SchoolWriteInput>(key: K, value: SchoolWriteInput[K]) {
    setForm((current) => ({ ...current, [key]: value }));
  }

  return (
    <div className="container-shell py-10 sm:py-14">
      <div className="flex flex-wrap items-end justify-between gap-5"><div><p className="eyebrow">Administration</p><h1 className="mt-3 text-4xl font-bold tracking-tight text-slate-950">School listings</h1><p className="mt-3 text-slate-600">Create, edit, approve, and remove schools in the directory.</p></div><Button onClick={() => { setEditingId(null); setForm({ ...emptyForm }); setEditorOpen(true); setError(""); }}><Plus size={17} /> Add school</Button></div>
      {error && <p role="alert" className="mt-5 rounded-xl bg-rose-50 p-4 text-sm text-rose-800">{error}</p>}
      {query.isError && <p role="alert" className="mt-5 rounded-xl bg-rose-50 p-4 text-sm text-rose-800">Could not load school listings.</p>}
      <div className="mt-8 overflow-hidden rounded-2xl border border-slate-200 bg-white">
        <Table><TableHeader><TableRow><TableHead>School</TableHead><TableHead>City</TableHead><TableHead>Type</TableHead><TableHead>Ownership</TableHead><TableHead>Status</TableHead><TableHead className="text-right">Actions</TableHead></TableRow></TableHeader><TableBody>{query.data?.items.map((school) => <TableRow key={school.id}><TableCell className="font-semibold text-slate-900">{school.name}</TableCell><TableCell>{school.city}</TableCell><TableCell>{schoolTypeLabels[school.schoolType]}</TableCell><TableCell>{school.isPrivate ? "Private" : "Public"}</TableCell><TableCell><span className={`status-pill ${school.isApproved ? "status-approved" : "status-draft"}`}>{school.isApproved ? "Approved" : "Draft"}</span></TableCell><TableCell><div className="flex justify-end gap-2"><Button variant="outline" size="sm" onClick={() => beginEdit(school)} aria-label={`Edit ${school.name}`}><Pencil size={15} /></Button><Button variant="outline" size="sm" className="text-rose-700 hover:bg-rose-50" onClick={() => { if (window.confirm(`Delete ${school.name}?`)) remove.mutate(school.id); }} aria-label={`Delete ${school.name}`}><Trash2 size={15} /></Button></div></TableCell></TableRow>)}{query.data?.items.length === 0 && <TableRow><TableCell colSpan={6} className="py-12 text-center text-slate-500">No listings yet. Add the first school to the directory.</TableCell></TableRow>}</TableBody></Table>
      </div>
      <div className="mt-5 flex items-center justify-between border-t border-slate-200 pt-4">
        <p className="text-sm text-slate-500">Page {page} of {pageCount}</p>
        <div className="flex gap-2">
          <Button variant="outline" size="sm" disabled={page <= 1 || query.isFetching} onClick={() => setPage((current) => current - 1)}><ChevronLeft size={16} /> Previous</Button>
          <Button variant="outline" size="sm" disabled={page >= pageCount || query.isFetching} onClick={() => setPage((current) => current + 1)}>Next <ChevronRight size={16} /></Button>
        </div>
      </div>
      <SchoolEditor
        open={editorOpen}
        title={editingId ? "Edit school listing" : "Add a school"}
        form={form}
        busy={save.isPending}
        onClose={() => { setEditingId(null); setForm(emptyForm); setEditorOpen(false); setError(""); }}
        onChange={change}
        onSubmit={(event) => { event.preventDefault(); save.mutate(); }}
      />
    </div>
  );
}

function SchoolEditor({
  open, title, form, busy, onClose, onChange, onSubmit,
}: {
  open: boolean;
  title: string;
  form: SchoolWriteInput;
  busy: boolean;
  onClose: () => void;
  onChange: <K extends keyof SchoolWriteInput>(key: K, value: SchoolWriteInput[K]) => void;
  onSubmit: (event: React.FormEvent<HTMLFormElement>) => void;
}) {
  if (!open) return null;
  return <div className="modal-backdrop" role="presentation"><form className="editor-modal" onSubmit={onSubmit}><div className="flex items-start justify-between"><div><p className="eyebrow">School details</p><h2>{title}</h2></div><button type="button" className="text-2xl leading-none text-slate-400" onClick={onClose} aria-label="Close dialog">×</button></div>
    <div className="mt-6 grid gap-4 sm:grid-cols-2">
      <div className="space-y-2 sm:col-span-2"><Label htmlFor="schoolName">Name</Label><Input id="schoolName" required value={form.name} onChange={(event) => onChange("name", event.target.value)} /></div>
      <div className="space-y-2"><Label htmlFor="schoolType">Type</Label><select id="schoolType" className="form-select" value={form.schoolType} onChange={(event) => onChange("schoolType", event.target.value as SchoolType)}>{schoolTypes.map((type) => <option key={type} value={type}>{schoolTypeLabels[type]}</option>)}</select></div>
      <div className="space-y-2"><Label htmlFor="schoolCity">City</Label><Input id="schoolCity" required value={form.city} onChange={(event) => onChange("city", event.target.value)} /></div>
      <div className="space-y-2 sm:col-span-2"><Label htmlFor="schoolAddress">Address</Label><Input id="schoolAddress" required value={form.address} onChange={(event) => onChange("address", event.target.value)} /></div>
      <div className="space-y-2"><Label htmlFor="schoolPostalCode">Postal code</Label><Input id="schoolPostalCode" value={form.postalCode} onChange={(event) => onChange("postalCode", event.target.value)} /></div>
      <div className="space-y-2"><Label htmlFor="tuitionFrom">Tuition from (€)</Label><Input id="tuitionFrom" type="number" min="0" value={form.tuitionFrom ?? ""} onChange={(event) => onChange("tuitionFrom", event.target.value === "" ? null : Number(event.target.value))} /></div>
      <div className="space-y-2"><Label htmlFor="tuitionTo">Tuition to (€)</Label><Input id="tuitionTo" type="number" min="0" value={form.tuitionTo ?? ""} onChange={(event) => onChange("tuitionTo", event.target.value === "" ? null : Number(event.target.value))} /></div>
      <div className="space-y-2"><Label htmlFor="schoolEmail">Contact email</Label><Input id="schoolEmail" type="email" value={form.email} onChange={(event) => onChange("email", event.target.value)} /></div>
      <div className="space-y-2"><Label htmlFor="schoolPhone">Phone</Label><Input id="schoolPhone" value={form.phone} onChange={(event) => onChange("phone", event.target.value)} /></div>
      <div className="space-y-2"><Label htmlFor="schoolWebsite">Website</Label><Input id="schoolWebsite" type="url" placeholder="https://" value={form.website} onChange={(event) => onChange("website", event.target.value)} /></div>
      <div className="space-y-2"><Label htmlFor="schoolLogo">Logo URL</Label><Input id="schoolLogo" type="url" placeholder="https://" value={form.logoUrl} onChange={(event) => onChange("logoUrl", event.target.value)} /></div>
      <div className="space-y-2"><Label htmlFor="studentCapacity">Student capacity</Label><Input id="studentCapacity" type="number" min="1" value={form.studentCapacity ?? ""} onChange={(event) => onChange("studentCapacity", event.target.value === "" ? null : Number(event.target.value))} /></div>
      <div className="space-y-2"><Label htmlFor="establishedYear">Established year</Label><Input id="establishedYear" type="number" min="1800" value={form.establishedYear ?? ""} onChange={(event) => onChange("establishedYear", event.target.value === "" ? null : Number(event.target.value))} /></div>
      <div className="space-y-2 sm:col-span-2"><Label htmlFor="schoolDescription">Description</Label><textarea id="schoolDescription" className="form-textarea" rows={3} value={form.description} onChange={(event) => onChange("description", event.target.value)} /></div>
      <div className="space-y-2 sm:col-span-2"><Label htmlFor="schoolPhotos">Photo URLs (one per line)</Label><textarea id="schoolPhotos" className="form-textarea" rows={3} value={form.photos.map((photo) => photo.url).join("\n")} onChange={(event) => onChange("photos", event.target.value.split(/\r?\n/).map((url) => url.trim()).filter(Boolean).map((url, sortOrder) => ({ url, sortOrder })))} /></div>
      <label className="flex items-center gap-2 text-sm font-medium"><input type="checkbox" checked={form.isPrivate} onChange={(event) => onChange("isPrivate", event.target.checked)} /> Private school</label>
      <label className="flex items-center gap-2 text-sm font-medium"><input type="checkbox" checked={form.isApproved} onChange={(event) => onChange("isApproved", event.target.checked)} /> Approved for public directory</label>
    </div>
    <div className="mt-7 flex justify-end gap-3"><Button type="button" variant="outline" onClick={onClose}>Cancel</Button><Button type="submit" disabled={busy}>{busy ? "Saving…" : "Save school"}</Button></div>
  </form></div>;
}
