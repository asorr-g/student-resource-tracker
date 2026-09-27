"use strict";

// ---------- Constants ----------
const TYPES = ["LectureNotes", "Textbook", "Video", "Article", "Assignment", "PastQuestion", "Link", "Other"];
const STATUSES = ["NotStarted", "InProgress", "Completed"];
const PRIORITIES = ["Low", "Medium", "High"];
const pretty = s => s.replace(/([a-z])([A-Z])/g, "$1 $2");

const state = { courses: [], resources: [], editingResourceId: null, editingCourseId: null };
const $ = sel => document.querySelector(sel);

// ---------- Helpers ----------
// Every piece of user text goes through esc() before touching innerHTML.
const esc = s => String(s ?? "").replace(/[&<>"']/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));

async function api(path, options = {}) {
  const res = await fetch("/api" + path, {
    headers: { "Content-Type": "application/json" },
    ...options,
    body: options.body ? JSON.stringify(options.body) : undefined
  });
  if (res.status === 204) return null;
  let data = null;
  try { data = await res.json(); } catch { /* no body */ }
  if (!res.ok) {
    const msg = data?.errors ? Object.values(data.errors).flat().join(" ")
              : data?.error || data?.title || `Request failed (${res.status})`;
    throw new Error(msg);
  }
  return data;
}

let toastTimer;
function toast(msg) {
  const t = $("#toast");
  t.textContent = msg; t.hidden = false;
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => (t.hidden = true), 3000);
}

function fillSelect(sel, items, { first, label = pretty } = {}) {
  sel.innerHTML = (first ? `<option value="">${esc(first)}</option>` : "") +
    items.map(i => typeof i === "string"
      ? `<option value="${esc(i)}">${esc(label(i))}</option>`
      : `<option value="${esc(i.value)}">${esc(i.text)}</option>`).join("");
}

function fmtDate(iso) {
  if (!iso) return "—";
  const [y, m, d] = iso.split("-").map(Number);
  return new Date(y, m - 1, d).toLocaleDateString(undefined, { day: "numeric", month: "short", year: "numeric" });
}

// ---------- Tabs ----------
document.querySelectorAll(".tab").forEach(btn => btn.addEventListener("click", () => showTab(btn.dataset.tab)));

function showTab(name) {
  document.querySelectorAll(".tab").forEach(b => b.classList.toggle("active", b.dataset.tab === name));
  document.querySelectorAll(".panel").forEach(p => (p.hidden = p.id !== "tab-" + name));
  if (name === "dashboard") loadDashboard();
  if (name === "resources") loadResources();
  if (name === "courses") loadCourses();
}

// ---------- Dashboard ----------
function barRow(label, value, max, { suffix = "", ok = false } = {}) {
  const pct = max > 0 ? Math.round((value / max) * 100) : 0;
  return `<div class="bar-row"><span class="lbl" title="${esc(label)}">${esc(label)}</span>
    <div class="bar ${ok ? "ok" : ""}"><span style="width:${pct}%"></span></div>
    <span class="val">${value}${suffix}</span></div>`;
}

async function loadDashboard() {
  try {
    const s = await api("/analytics/summary");
    $("#kpis").innerHTML = [
      ["Courses", s.totalCourses, ""],
      ["Resources", s.totalResources, ""],
      ["Completion", s.completionRate + "%", "ok"],
      ["In progress", s.inProgress, ""],
      ["Due in 7 days", s.dueWithin7Days, s.dueWithin7Days ? "warn" : ""],
      ["Overdue", s.overdue, s.overdue ? "bad" : ""]
    ].map(([l, n, c]) => `<div class="kpi ${c}"><div class="n">${esc(n)}</div><div class="l">${l}</div></div>`).join("");

    $("#byCourse").innerHTML = s.byCourse.length
      ? s.byCourse.map(c => barRow(`${c.courseCode} · ${c.completed}/${c.total}`, c.completionRate, 100, { suffix: "%", ok: true })).join("")
      : `<p class="empty">No courses yet.</p>`;

    const maxType = Math.max(0, ...s.byType.map(t => t.count));
    $("#byType").innerHTML = s.byType.length
      ? s.byType.map(t => barRow(pretty(t.label), t.count, maxType)).join("")
      : `<p class="empty">No resources yet.</p>`;

    const maxPri = Math.max(0, ...s.openByPriority.map(t => t.count));
    $("#byPriority").innerHTML = s.openByPriority.map(t => barRow(t.label, t.count, maxPri)).join("");

    $("#upcoming").innerHTML = s.upcoming.length
      ? s.upcoming.map(u => {
          const cls = u.daysLeft < 0 ? "overdue" : u.daysLeft <= 3 ? "soon" : "";
          const when = u.daysLeft < 0 ? `${-u.daysLeft}d overdue` : u.daysLeft === 0 ? "Today" : `in ${u.daysLeft}d`;
          return `<div class="upc"><div>${esc(u.title)}<small>${esc(u.courseCode)} · ${pretty(u.priority)} priority</small></div>
                  <div class="when ${cls}">${when}<small>${fmtDate(u.dueDate)}</small></div></div>`;
        }).join("")
      : `<p class="empty">Nothing due. 🎉</p>`;
  } catch (e) { toast(e.message); }
}

// ---------- Courses ----------
async function loadCourses() {
  try {
    state.courses = await api("/courses");
    renderCourses();
    refreshCourseSelects();
  } catch (e) { toast(e.message); }
}

function renderCourses() {
  $("#course-empty").hidden = state.courses.length > 0;
  $("#course-rows").innerHTML = state.courses.map(c => `
    <tr>
      <td><strong>${esc(c.code)}</strong></td><td>${esc(c.title)}</td>
      <td>${esc(c.lecturer || "—")}</td><td>${esc(c.semester || "—")}</td><td>${c.resourceCount}</td>
      <td style="white-space:nowrap">
        <button class="btn sm" data-edit-course="${c.id}">Edit</button>
        <button class="btn sm danger" data-del-course="${c.id}">Delete</button>
      </td>
    </tr>`).join("");
}

function refreshCourseSelects() {
  const opts = state.courses.map(c => ({ value: c.id, text: `${c.code} — ${c.title}` }));
  const filter = $("#f-course"), current = filter.value;
  fillSelect(filter, opts, { first: "All courses" });
  filter.value = current;
  fillSelect($("#form-resource [name=courseId]"), opts);
}

$("#btn-add-course").addEventListener("click", () => openCourseDialog());
$("#course-rows").addEventListener("click", async e => {
  const edit = e.target.closest("[data-edit-course]"), del = e.target.closest("[data-del-course]");
  if (edit) openCourseDialog(state.courses.find(c => c.id === edit.dataset.editCourse));
  if (del) {
    const c = state.courses.find(x => x.id === del.dataset.delCourse);
    const extra = c.resourceCount ? ` This also deletes its ${c.resourceCount} resource(s).` : "";
    if (!confirm(`Delete course ${c.code}?${extra}`)) return;
    try { await api(`/courses/${c.id}`, { method: "DELETE" }); toast("Course deleted"); loadCourses(); }
    catch (err) { toast(err.message); }
  }
});

function openCourseDialog(course) {
  const f = $("#form-course");
  state.editingCourseId = course?.id ?? null;
  $("#dlg-course-title").textContent = course ? "Edit course" : "Add course";
  f.code.value = course?.code ?? ""; f.title.value = course?.title ?? "";
  f.lecturer.value = course?.lecturer ?? ""; f.semester.value = course?.semester ?? "";
  $("#course-error").hidden = true;
  $("#dlg-course").showModal();
}

$("#form-course").addEventListener("submit", async e => {
  e.preventDefault();
  const f = e.target;
  const body = { code: f.code.value, title: f.title.value, lecturer: f.lecturer.value, semester: f.semester.value };
  try {
    if (state.editingCourseId) await api(`/courses/${state.editingCourseId}`, { method: "PUT", body });
    else await api("/courses", { method: "POST", body });
    $("#dlg-course").close();
    toast("Course saved");
    loadCourses();
  } catch (err) { const el = $("#course-error"); el.textContent = err.message; el.hidden = false; }
});

// ---------- Resources ----------
function currentFilters() {
  const p = new URLSearchParams();
  const add = (k, v) => v && p.set(k, v);
  add("q", $("#f-q").value.trim()); add("courseId", $("#f-course").value);
  add("type", $("#f-type").value); add("status", $("#f-status").value); add("priority", $("#f-priority").value);
  if ($("#f-overdue").checked) p.set("overdue", "true");
  const qs = p.toString();
  return qs ? "?" + qs : "";
}

async function loadResources() {
  try {
    if (!state.courses.length) await loadCourses();
    state.resources = await api("/resources" + currentFilters());
    renderResources();
  } catch (e) { toast(e.message); }
}

function renderResources() {
  $("#resource-empty").hidden = state.resources.length > 0;
  $("#resource-rows").innerHTML = state.resources.map(r => {
    const title = r.url
      ? `<a href="${esc(r.url)}" target="_blank" rel="noopener noreferrer" class="t-title">${esc(r.title)} ↗</a>`
      : `<span class="t-title">${esc(r.title)}</span>`;
    const tags = r.tags.map(t => `<span class="tag">${esc(t)}</span>`).join("");
    const statusSel = `<select data-status="${r.id}">${STATUSES.map(s =>
      `<option value="${s}" ${s === r.status ? "selected" : ""}>${pretty(s)}</option>`).join("")}</select>`;
    return `<tr class="${r.status === "Completed" ? "done" : ""}">
      <td>${title}${r.notes ? `<div class="t-sub">${esc(r.notes.length > 90 ? r.notes.slice(0, 90) + "…" : r.notes)}</div>` : ""}<div>${tags}</div></td>
      <td>${esc(r.courseCode)}</td>
      <td>${pretty(r.type)}</td>
      <td><span class="pill ${r.priority}">${r.priority}</span></td>
      <td class="due ${r.isOverdue ? "overdue" : ""}">${fmtDate(r.dueDate)}${r.isOverdue ? "<div class='t-sub'>Overdue</div>" : ""}</td>
      <td>${statusSel}</td>
      <td style="white-space:nowrap">
        <button class="btn sm" data-edit="${r.id}">Edit</button>
        <button class="btn sm danger" data-del="${r.id}">Delete</button>
      </td>
    </tr>`;
  }).join("");
}

// Filters
fillSelect($("#f-type"), TYPES, { first: "All types" });
fillSelect($("#f-status"), STATUSES, { first: "All statuses" });
fillSelect($("#f-priority"), PRIORITIES, { first: "All priorities" });
fillSelect($("#form-resource [name=type]"), TYPES);
fillSelect($("#form-resource [name=status]"), STATUSES);
fillSelect($("#form-resource [name=priority]"), PRIORITIES);
$("#form-resource [name=priority]").value = "Medium";

let searchTimer;
$("#f-q").addEventListener("input", () => { clearTimeout(searchTimer); searchTimer = setTimeout(loadResources, 250); });
["#f-course", "#f-type", "#f-status", "#f-priority", "#f-overdue"].forEach(s => $(s).addEventListener("change", loadResources));

$("#resource-rows").addEventListener("change", async e => {
  const sel = e.target.closest("[data-status]");
  if (!sel) return;
  try { await api(`/resources/${sel.dataset.status}/status`, { method: "PATCH", body: { status: sel.value } }); loadResources(); }
  catch (err) { toast(err.message); }
});

$("#resource-rows").addEventListener("click", async e => {
  const edit = e.target.closest("[data-edit]"), del = e.target.closest("[data-del]");
  if (edit) openResourceDialog(state.resources.find(r => r.id === edit.dataset.edit));
  if (del) {
    const r = state.resources.find(x => x.id === del.dataset.del);
    if (!confirm(`Delete "${r.title}"?`)) return;
    try { await api(`/resources/${r.id}`, { method: "DELETE" }); toast("Resource deleted"); loadResources(); }
    catch (err) { toast(err.message); }
  }
});

$("#btn-add-resource").addEventListener("click", () => {
  if (!state.courses.length) { toast("Add a course first."); showTab("courses"); return; }
  openResourceDialog();
});

function openResourceDialog(r) {
  const f = $("#form-resource");
  state.editingResourceId = r?.id ?? null;
  $("#dlg-resource-title").textContent = r ? "Edit resource" : "Add resource";
  f.title.value = r?.title ?? "";
  f.courseId.value = r?.courseId ?? (state.courses[0]?.id ?? "");
  f.type.value = r?.type ?? "LectureNotes";
  f.status.value = r?.status ?? "NotStarted";
  f.priority.value = r?.priority ?? "Medium";
  f.dueDate.value = r?.dueDate ?? "";
  f.url.value = r?.url ?? "";
  f.tags.value = (r?.tags ?? []).join(", ");
  f.notes.value = r?.notes ?? "";
  $("#resource-error").hidden = true;
  $("#dlg-resource").showModal();
}

$("#form-resource").addEventListener("submit", async e => {
  e.preventDefault();
  const f = e.target;
  const body = {
    courseId: f.courseId.value, title: f.title.value, type: f.type.value, status: f.status.value,
    priority: f.priority.value, dueDate: f.dueDate.value || null, url: f.url.value, notes: f.notes.value,
    tags: f.tags.value.split(",")
  };
  try {
    if (state.editingResourceId) await api(`/resources/${state.editingResourceId}`, { method: "PUT", body });
    else await api("/resources", { method: "POST", body });
    $("#dlg-resource").close();
    toast("Resource saved");
    loadResources();
  } catch (err) { const el = $("#resource-error"); el.textContent = err.message; el.hidden = false; }
});

// Dialog cancel buttons
document.querySelectorAll("[data-close]").forEach(b => b.addEventListener("click", () => b.closest("dialog").close()));

// ---------- Start ----------
loadCourses().then(loadDashboard);
