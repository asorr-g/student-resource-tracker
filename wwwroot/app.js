"use strict";

// =====================================================================
// Constants
// =====================================================================
const TYPES = ["LectureNotes", "Textbook", "Video", "Article", "Assignment", "PastQuestion", "Link", "Other"];
const STATUSES = ["NotStarted", "InProgress", "Completed"];
const PRIORITIES = ["Low", "Medium", "High"];
// Kept inside the blue band so courses and types stay on-brand (UG navy and white).
const TYPE_HUES = { LectureNotes: 212, Textbook: 198, Video: 236, Article: 190, Assignment: 222, PastQuestion: 246, Link: 204, Other: 216 };
const COURSE_HUES = [212, 194, 232, 204, 244, 220, 186, 228, 200, 238];
const STATUS_DOT = { NotStarted: "var(--muted)", InProgress: "var(--info)", Completed: "var(--ok)" };
const RING_CIRCUMFERENCE = 2 * Math.PI * 52;

const state = {
  route: "overview",
  courses: [],
  resources: [],
  summary: null,
  filters: { q: "", courseId: "", type: "", status: "", priority: "", overdue: false },
  editingResourceId: null,
  editingCourseId: null
};

// =====================================================================
// Helpers
// =====================================================================
const $ = (sel, root = document) => root.querySelector(sel);
const $$ = (sel, root = document) => [...root.querySelectorAll(sel)];
const pretty = s => String(s ?? "").replace(/([a-z])([A-Z])/g, "$1 $2");
const plural = (n, word) => `${n} ${word}${n === 1 ? "" : "s"}`;
const icon = (id, cls = "i") => `<svg class="${cls}"><use href="#${id}"/></svg>`;

// Every piece of user text goes through esc() before touching innerHTML.
const esc = s => String(s ?? "").replace(/[&<>"']/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));

function courseHue(code) {
  let h = 0;
  for (const ch of String(code ?? "")) h = (h * 31 + ch.charCodeAt(0)) >>> 0;
  return COURSE_HUES[h % COURSE_HUES.length];
}

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

function toast(message, kind = "ok") {
  const el = document.createElement("div");
  el.className = `toast ${kind}`;
  el.innerHTML = `${icon(kind === "ok" ? "i-check" : "i-alert")}<span>${esc(message)}</span>`;
  $("#toasts").append(el);
  setTimeout(() => { el.classList.add("out"); el.addEventListener("animationend", () => el.remove()); }, 3200);
}

function animateBars(root) {
  requestAnimationFrame(() => requestAnimationFrame(() =>
    $$("[data-w]", root).forEach(el => (el.style.width = el.dataset.w + "%"))));
}

// ---------- Dates (YYYY-MM-DD strings, compared in local time) ----------
function parseDate(iso) {
  const [y, m, d] = iso.split("-").map(Number);
  return new Date(y, m - 1, d);
}
function daysFromToday(iso) {
  const t = new Date(); t.setHours(0, 0, 0, 0);
  return Math.round((parseDate(iso) - t) / 86400000);
}
const fmtDate = (iso, opts = { day: "numeric", month: "short" }) => parseDate(iso).toLocaleDateString(undefined, opts);

function dueInfo(r) {
  if (!r.dueDate) return { cls: "none", label: "No date", sub: "" };
  const days = daysFromToday(r.dueDate);
  const sameYear = parseDate(r.dueDate).getFullYear() === new Date().getFullYear();
  const label = fmtDate(r.dueDate, sameYear ? { day: "numeric", month: "short" } : { day: "numeric", month: "short", year: "numeric" });
  if (r.status === "Completed") return { cls: "", label, sub: "Done" };
  if (days < 0) return { cls: "overdue", label, sub: `${plural(-days, "day")} overdue` };
  if (days === 0) return { cls: "soon", label, sub: "Due today" };
  if (days === 1) return { cls: "soon", label, sub: "Tomorrow" };
  if (days <= 3) return { cls: "soon", label, sub: `In ${days} days` };
  return { cls: "", label, sub: `In ${days} days` };
}

function whenText(daysLeft) {
  if (daysLeft < 0) return `${plural(-daysLeft, "day")} late`;
  if (daysLeft === 0) return "Today";
  if (daysLeft === 1) return "Tomorrow";
  return `In ${daysLeft} days`;
}

// =====================================================================
// Routing
// =====================================================================
const ROUTES = ["overview", "resources", "courses"];

function route() {
  const name = ROUTES.includes(location.hash.slice(1)) ? location.hash.slice(1) : "overview";
  state.route = name;
  $$(".page").forEach(p => (p.hidden = p.dataset.page !== name));
  $$(".nav-item").forEach(a => a.classList.toggle("active", a.dataset.route === name));
  document.title = `${name[0].toUpperCase() + name.slice(1)} · Academic Resource Tracker`;
  window.scrollTo({ top: 0 });
  return refresh();
}

async function refresh() {
  try {
    await Promise.all([loadCourses(), loadSummary()]);
    if (state.route === "overview") renderOverview();
    if (state.route === "resources") { renderStatusFilter(); await loadResources(); }
    if (state.route === "courses") renderCourses();
  } catch (e) { toast(e.message, "err"); }
}

// =====================================================================
// Data loading
// =====================================================================
async function loadCourses() {
  state.courses = await api("/courses");
  renderSidebar();
  refreshCourseSelects();
}

async function loadSummary() {
  state.summary = await api("/analytics/summary");
  $("#nav-count-resources").textContent = state.summary.totalResources || "";
}

function filterQuery() {
  const f = state.filters, p = new URLSearchParams();
  if (f.q) p.set("q", f.q);
  for (const k of ["courseId", "type", "status", "priority"]) if (f[k]) p.set(k, f[k]);
  if (f.overdue) p.set("overdue", "true");
  const qs = p.toString();
  return qs ? "?" + qs : "";
}

const filtersActive = () => { const f = state.filters; return !!(f.q || f.courseId || f.type || f.status || f.priority || f.overdue); };

async function loadResources() {
  state.resources = await api("/resources" + filterQuery());
  renderResources();
}

// =====================================================================
// Sidebar
// =====================================================================
function renderSidebar() {
  $("#nav-count-courses").textContent = state.courses.length || "";
  $("#side-courses").innerHTML = state.courses.length
    ? state.courses.map(c => `
        <button class="side-course hue" style="--hue:${courseHue(c.code)}" data-side-course="${esc(c.id)}" title="${esc(c.code)} · ${esc(c.title)}">
          <span class="cdot"></span><span class="name">${esc(c.code)}</span><span class="count">${c.resourceCount}</span>
        </button>`).join("")
    : `<p class="side-empty">No courses yet</p>`;
}

$("#side-courses").addEventListener("click", e => {
  const b = e.target.closest("[data-side-course]");
  if (b) showCourseResources(b.dataset.sideCourse);
});

function showCourseResources(courseId) {
  Object.assign(state.filters, { q: "", courseId, type: "", status: "", priority: "", overdue: false });
  syncFilterControls();
  if (location.hash === "#resources") route(); else location.hash = "resources";
}

// =====================================================================
// Overview
// =====================================================================
function renderOverview() {
  const s = state.summary;
  const hour = new Date().getHours();
  $("#greeting").textContent = hour < 12 ? "Good morning" : hour < 18 ? "Good afternoon" : "Good evening";
  $("#today-label").textContent = new Date().toLocaleDateString(undefined, { weekday: "long", day: "numeric", month: "long" });

  const open = s.totalResources - s.completed;
  let headline;
  if (!s.totalResources) headline = s.totalCourses ? "Add your first resource to start tracking progress." : "Start by adding a course, then the materials you need to get through.";
  else if (!open) headline = `Everything is done. <strong>All ${plural(s.totalResources, "resource")}</strong> completed.`;
  else {
    const parts = [];
    if (s.overdue) parts.push(`<strong>${plural(s.overdue, "overdue item")}</strong>`);
    if (s.dueWithin7Days) parts.push(`<strong>${s.dueWithin7Days} due</strong> in the next 7 days`);
    headline = parts.length
      ? `You have ${parts.join(" and ")}. ${s.completed} of ${s.totalResources} resources completed so far.`
      : `Nothing due this week. ${s.completed} of ${s.totalResources} resources completed so far.`;
  }
  $("#headline").innerHTML = headline;

  $("#ring-pct").textContent = Math.round(s.completionRate) + "%";
  requestAnimationFrame(() => ($("#ring-fill").style.strokeDashoffset = RING_CIRCUMFERENCE * (1 - s.completionRate / 100)));

  const stat = (cls, label, value, sub = "") =>
    `<div class="stat ${cls}"><dt><span class="dot"></span>${label}</dt><dd>${value}${sub ? `<small>${sub}</small>` : ""}</dd></div>`;
  $("#stats").innerHTML =
    stat("prog", "In progress", s.inProgress) +
    stat("idle", "Not started", s.notStarted) +
    stat(`warn ${s.dueWithin7Days ? "on" : ""}`, "Due in 7 days", s.dueWithin7Days) +
    stat(`bad ${s.overdue ? "on" : ""}`, "Overdue", s.overdue);

  renderDeadlines(s);
  renderCourseProgress(s);
  renderTypeMix(s);

  $("#priority-mix").innerHTML = s.openByPriority.map(p => `
    <div class="pm ${p.label}"><div class="pm-label">${icon("i-flag")}${p.label}</div><b>${p.count}</b></div>`).join("");
}

function renderDeadlines(s) {
  const item = u => {
    const cls = u.daysLeft < 0 ? "overdue" : u.daysLeft <= 3 ? "soon" : "";
    const d = parseDate(u.dueDate);
    return `
      <button class="dl ${cls}" data-open-resource="${esc(u.id)}">
        <span class="dl-date"><b>${d.getDate()}</b><small>${d.toLocaleDateString(undefined, { month: "short" })}</small></span>
        <span class="dl-main">
          <span class="dl-title" title="${esc(u.title)}">${esc(u.title)}</span>
          <span class="dl-meta">
            <span class="course-chip hue" style="--hue:${courseHue(u.courseCode)}"><span class="cdot"></span>${esc(u.courseCode)}</span>
            <span class="prio ${u.priority}">${icon("i-flag")}${u.priority}</span>
          </span>
        </span>
        <span class="dl-when">${whenText(u.daysLeft)}</span>
      </button>`;
  };

  const thisWeek = s.upcoming.filter(u => u.daysLeft <= 6);
  const later = s.upcoming.filter(u => u.daysLeft > 6);
  const extraOverdue = s.overdue - s.overdueItems.length;
  let html = "";
  if (s.overdueItems.length) html += `<div class="dl-group bad">Overdue</div>${s.overdueItems.map(item).join("")}` +
    (extraOverdue > 0 ? `<p class="muted small" style="padding:6px 0">and ${extraOverdue} more overdue</p>` : "");
  if (thisWeek.length) html += `<div class="dl-group">This week</div>${thisWeek.map(item).join("")}`;
  if (later.length) html += `<div class="dl-group">Later</div>${later.map(item).join("")}`;

  $("#deadlines").innerHTML = html || emptyState("i-calendar", "No upcoming deadlines",
    "Resources with a due date will show up here.", true);
}

function renderCourseProgress(s) {
  $("#course-progress").innerHTML = s.byCourse.length
    ? s.byCourse.map(c => `
        <div class="cp hue" style="--hue:${courseHue(c.courseCode)}">
          <div class="cp-name"><span class="cdot"></span><span>${esc(c.courseCode)}</span><span>${esc(c.courseTitle)}</span></div>
          <div class="cp-val">${c.completed}/${c.total}</div>
          <div class="bar"><i data-w="${c.completionRate}"></i></div>
        </div>`).join("")
    : emptyState("i-courses", "No courses yet", "Create a course to see progress here.", true);
  animateBars($("#course-progress"));
}

function renderTypeMix(s) {
  const total = s.byType.reduce((n, t) => n + t.count, 0);
  $("#mix-total").textContent = total ? plural(total, "item") : "";
  $("#type-mix").innerHTML = total
    ? `<div class="mix-bar">${s.byType.map(t =>
        `<i class="hue" style="--hue:${TYPE_HUES[t.label] ?? 225};width:${(t.count / total) * 100}%;background:var(--c)" title="${esc(pretty(t.label))}: ${t.count}"></i>`).join("")}</div>
       <div class="mix-legend">${s.byType.map(t =>
        `<div class="hue" style="--hue:${TYPE_HUES[t.label] ?? 225}"><span class="sw" style="background:var(--c)"></span>${esc(pretty(t.label))}<b>${t.count}</b></div>`).join("")}</div>`
    : emptyState("i-resources", "Nothing in your library", "Add resources to see the mix.", true);
}

$("#deadlines").addEventListener("click", async e => {
  const b = e.target.closest("[data-open-resource]");
  if (!b) return;
  try {
    const all = await api("/resources");
    const r = all.find(x => x.id === b.dataset.openResource);
    if (r) openResourceDialog(r);
  } catch (err) { toast(err.message, "err"); }
});

// =====================================================================
// Resources
// =====================================================================
function renderStatusFilter() {
  const s = state.summary;
  const counts = { "": s.totalResources, NotStarted: s.notStarted, InProgress: s.inProgress, Completed: s.completed };
  const opts = [["", "All"], ...STATUSES.map(x => [x, pretty(x)])];
  $("#f-status").innerHTML = opts.map(([v, label]) => `
    <label><input type="radio" name="f-status" value="${v}" ${state.filters.status === v ? "checked" : ""}>
      <span>${v ? `<i class="dot" style="background:${STATUS_DOT[v]}"></i>` : ""}${label}<em>${counts[v]}</em></span></label>`).join("");
}

function renderResources() {
  const rows = state.resources;
  const n = rows.length;
  $("#result-count").textContent = filtersActive() ? `${plural(n, "result")}` : plural(n, "resource");
  $("#btn-clear").hidden = !filtersActive();

  $("#resource-rows").innerHTML = rows.map(r => {
    const due = dueInfo(r);
    const title = r.url
      ? `<a class="r-title" href="${esc(r.url)}" target="_blank" rel="noopener noreferrer">${esc(r.title)}${icon("i-external")}</a>`
      : `<span class="r-title">${esc(r.title)}</span>`;
    const tags = r.tags.map(t => `<span class="tag">${esc(t)}</span>`).join("");
    return `
      <tr class="${r.status === "Completed" ? "done" : ""}">
        <td>
          <div class="r-cell">
            <span class="type-tile hue" style="--hue:${TYPE_HUES[r.type] ?? 225}" title="${esc(pretty(r.type))}">${icon("t-" + (TYPES.includes(r.type) ? r.type : "Other"))}</span>
            <div class="r-body">
              ${title}
              ${r.notes ? `<div class="r-sub">${esc(r.notes)}</div>` : ""}
              <div class="r-meta"><span class="r-type">${esc(pretty(r.type))}</span>${tags}</div>
            </div>
          </div>
        </td>
        <td class="c-course"><span class="course-chip hue" style="--hue:${courseHue(r.courseCode)}" title="${esc(r.courseTitle)}"><span class="cdot"></span>${esc(r.courseCode)}</span></td>
        <td class="c-prio"><span class="prio ${esc(r.priority)}">${icon("i-flag")}${esc(r.priority)}</span></td>
        <td class="c-due"><div class="due ${due.cls}"><b>${due.label}</b>${due.sub ? `<span>${due.sub}</span>` : ""}</div></td>
        <td class="c-status">
          <select class="status-select ${esc(r.status)}" data-status="${esc(r.id)}" aria-label="Status of ${esc(r.title)}">
            ${STATUSES.map(s => `<option value="${s}" ${s === r.status ? "selected" : ""}>${pretty(s)}</option>`).join("")}
          </select>
        </td>
        <td>
          <div class="row-actions">
            <button class="icon-btn" data-edit="${esc(r.id)}" title="Edit" aria-label="Edit ${esc(r.title)}">${icon("i-edit")}</button>
            <button class="icon-btn danger" data-del="${esc(r.id)}" title="Delete" aria-label="Delete ${esc(r.title)}">${icon("i-trash")}</button>
          </div>
        </td>
      </tr>`;
  }).join("");

  const empty = $("#resource-empty");
  empty.hidden = n > 0;
  if (!n) {
    empty.innerHTML = filtersActive()
      ? emptyInner("i-search", "Nothing matches these filters", "Try a different search or clear the filters.",
          `<button class="btn" data-action="clear-filters">Clear filters</button>`)
      : emptyInner("i-inbox", "Your library is empty", "Add lecture notes, readings, videos and assignments to start tracking them.",
          `<button class="btn primary" data-action="new-resource">${icon("i-plus")}New resource</button>`);
  }
}

function emptyInner(iconId, title, text, action = "") {
  return `<div class="empty-art">${icon(iconId)}</div><h3>${title}</h3><p>${text}</p>${action}`;
}
const emptyState = (iconId, title, text, compact = false, action = "") =>
  `<div class="empty ${compact ? "compact" : ""}">${emptyInner(iconId, title, text, action)}</div>`;

// ---------- Filters ----------
function fillSelect(sel, items, { first, label = pretty } = {}) {
  sel.innerHTML = (first !== undefined ? `<option value="">${esc(first)}</option>` : "") +
    items.map(i => typeof i === "string"
      ? `<option value="${esc(i)}">${esc(label(i))}</option>`
      : `<option value="${esc(i.value)}">${esc(i.text)}</option>`).join("");
}

function refreshCourseSelects() {
  const opts = state.courses.map(c => ({ value: c.id, text: `${c.code} · ${c.title}` }));
  fillSelect($("#f-course"), opts, { first: "All courses" });
  if (!state.courses.some(c => c.id === state.filters.courseId)) state.filters.courseId = "";
  $("#f-course").value = state.filters.courseId;
  const formCourse = $("#form-resource [name=courseId]"), current = formCourse.value;
  fillSelect(formCourse, opts);
  if (current) formCourse.value = current;
}

function syncFilterControls() {
  const f = state.filters;
  $("#f-q").value = f.q;
  $("#f-course").value = f.courseId;
  $("#f-type").value = f.type;
  $("#f-priority").value = f.priority;
  $("#f-overdue").checked = f.overdue;
  $$("#f-status input").forEach(i => (i.checked = i.value === f.status));
}

fillSelect($("#f-type"), TYPES, { first: "All types" });
fillSelect($("#f-priority"), PRIORITIES, { first: "All priorities" });

let searchTimer;
$("#f-q").addEventListener("input", e => {
  clearTimeout(searchTimer);
  searchTimer = setTimeout(() => { state.filters.q = e.target.value.trim(); loadResources().catch(err => toast(err.message, "err")); }, 220);
});
for (const [id, key] of [["#f-course", "courseId"], ["#f-type", "type"], ["#f-priority", "priority"]])
  $(id).addEventListener("change", e => { state.filters[key] = e.target.value; loadResources().catch(err => toast(err.message, "err")); });
$("#f-overdue").addEventListener("change", e => { state.filters.overdue = e.target.checked; loadResources().catch(err => toast(err.message, "err")); });
$("#f-status").addEventListener("change", e => { state.filters.status = e.target.value; loadResources().catch(err => toast(err.message, "err")); });

function clearFilters() {
  Object.assign(state.filters, { q: "", courseId: "", type: "", status: "", priority: "", overdue: false });
  syncFilterControls();
  loadResources().catch(err => toast(err.message, "err"));
}
$("#btn-clear").addEventListener("click", clearFilters);

// ---------- Row actions ----------
$("#resource-rows").addEventListener("change", async e => {
  const sel = e.target.closest("[data-status]");
  if (!sel) return;
  sel.className = `status-select ${sel.value}`;
  try {
    await api(`/resources/${sel.dataset.status}/status`, { method: "PATCH", body: { status: sel.value } });
    toast(sel.value === "Completed" ? "Marked as completed" : `Moved to ${pretty(sel.value).toLowerCase()}`);
    await loadSummary();
    renderStatusFilter();
    await loadResources();
  } catch (err) { toast(err.message, "err"); }
});

$("#resource-rows").addEventListener("click", async e => {
  const edit = e.target.closest("[data-edit]"), del = e.target.closest("[data-del]");
  if (edit) openResourceDialog(state.resources.find(r => r.id === edit.dataset.edit));
  if (del) {
    const r = state.resources.find(x => x.id === del.dataset.del);
    if (!await confirmDialog("Delete this resource?", `“${r.title}” will be permanently removed.`)) return;
    try { await api(`/resources/${r.id}`, { method: "DELETE" }); toast("Resource deleted"); refresh(); }
    catch (err) { toast(err.message, "err"); }
  }
});

// =====================================================================
// Courses
// =====================================================================
function renderCourses() {
  const progress = new Map((state.summary?.byCourse ?? []).map(c => [c.courseCode, c]));
  const cards = state.courses.map(c => {
    const p = progress.get(c.code) ?? { total: c.resourceCount, completed: 0, completionRate: 0 };
    const open = p.total - p.completed;
    const meta = [
      c.lecturer ? `<span>${icon("i-user")}${esc(c.lecturer)}</span>` : "",
      c.semester ? `<span>${icon("i-calendar")}${esc(c.semester)}</span>` : ""
    ].join("");
    return `
      <article class="course-card hue" style="--hue:${courseHue(c.code)}">
        <div class="cc-top">
          <span class="cc-code">${esc(c.code)}</span>
          <div class="cc-actions">
            <button class="icon-btn" data-edit-course="${esc(c.id)}" title="Edit" aria-label="Edit ${esc(c.code)}">${icon("i-edit")}</button>
            <button class="icon-btn danger" data-del-course="${esc(c.id)}" title="Delete" aria-label="Delete ${esc(c.code)}">${icon("i-trash")}</button>
          </div>
        </div>
        <h3 class="cc-title">${esc(c.title)}</h3>
        ${meta ? `<div class="cc-meta">${meta}</div>` : ""}
        <div class="cc-progress">
          <div class="cc-progress-head"><span><b>${Math.round(p.completionRate)}%</b> complete</span><span>${p.completed} of ${p.total}</span></div>
          <div class="bar"><i data-w="${p.completionRate}"></i></div>
        </div>
        <div class="cc-foot">
          <span>${p.total ? (open ? `${open} open` : "All done") : "No resources yet"}</span>
          <a class="link" href="#resources" data-view-course="${esc(c.id)}">View resources ${icon("i-arrow")}</a>
        </div>
      </article>`;
  }).join("");

  $("#course-grid").innerHTML = cards + `
    <button class="course-add" data-action="new-course">
      <span class="empty-art">${icon("i-plus")}</span>
      ${state.courses.length ? "Add another course" : "Add your first course"}
    </button>`;
  animateBars($("#course-grid"));
}

$("#course-grid").addEventListener("click", async e => {
  const view = e.target.closest("[data-view-course]");
  if (view) { e.preventDefault(); showCourseResources(view.dataset.viewCourse); return; }
  const edit = e.target.closest("[data-edit-course]"), del = e.target.closest("[data-del-course]");
  if (edit) openCourseDialog(state.courses.find(c => c.id === edit.dataset.editCourse));
  if (del) {
    const c = state.courses.find(x => x.id === del.dataset.delCourse);
    const extra = c.resourceCount ? ` Its ${plural(c.resourceCount, "resource")} will be deleted too.` : "";
    if (!await confirmDialog(`Delete ${c.code}?`, `“${c.title}” will be permanently removed.${extra}`)) return;
    try { await api(`/courses/${c.id}`, { method: "DELETE" }); toast("Course deleted"); refresh(); }
    catch (err) { toast(err.message, "err"); }
  }
});

// =====================================================================
// Dialogs
// =====================================================================
function segmented(container, name, values, labelFn = pretty, dotFn) {
  container.innerHTML = values.map(v => `
    <label><input type="radio" name="${name}" value="${v}"><span>${dotFn ? `<i class="dot" style="background:${dotFn(v)}"></i>` : ""}${labelFn(v)}</span></label>`).join("");
}
segmented($("#fr-status"), "status", STATUSES, pretty, v => STATUS_DOT[v]);
segmented($("#fr-priority"), "priority", PRIORITIES, v => v,
  v => ({ High: "var(--danger)", Medium: "var(--warn)", Low: "var(--info)" }[v]));
fillSelect($("#form-resource [name=type]"), TYPES);

function openResourceDialog(r) {
  if (!state.courses.length) { toast("Add a course first", "err"); location.hash = "courses"; return; }
  const f = $("#form-resource");
  state.editingResourceId = r?.id ?? null;
  $("#dlg-resource-title").textContent = r ? "Edit resource" : "New resource";
  $("#resource-submit").textContent = r ? "Save changes" : "Add resource";
  f.title.value = r?.title ?? "";
  f.courseId.value = r?.courseId ?? (state.filters.courseId || state.courses[0].id);
  f.type.value = r?.type ?? "LectureNotes";
  f.status.value = r?.status ?? "NotStarted";
  f.priority.value = r?.priority ?? "Medium";
  f.dueDate.value = r?.dueDate ?? "";
  f.url.value = r?.url ?? "";
  f.tags.value = (r?.tags ?? []).join(", ");
  f.notes.value = r?.notes ?? "";
  updateNotesCount();
  $("#resource-error").hidden = true;
  $("#dlg-resource").showModal();
  f.title.focus();
}

function updateNotesCount() {
  $("#notes-count").textContent = `${$("#form-resource").notes.value.length} / 2000`;
}
$("#form-resource").notes.addEventListener("input", updateNotesCount);

$("#form-resource").addEventListener("submit", async e => {
  e.preventDefault();
  const f = e.target, btn = $("#resource-submit");
  if (!f.title.value.trim()) return showFormError("#resource-error", "Please give the resource a title.");
  const body = {
    courseId: f.courseId.value, title: f.title.value, type: f.type.value, status: f.status.value,
    priority: f.priority.value, dueDate: f.dueDate.value || null, url: f.url.value, notes: f.notes.value,
    tags: f.tags.value.split(",")
  };
  btn.disabled = true;
  try {
    if (state.editingResourceId) await api(`/resources/${state.editingResourceId}`, { method: "PUT", body });
    else await api("/resources", { method: "POST", body });
    $("#dlg-resource").close();
    toast(state.editingResourceId ? "Changes saved" : "Resource added");
    refresh();
  } catch (err) { showFormError("#resource-error", err.message); }
  finally { btn.disabled = false; }
});

function openCourseDialog(course) {
  const f = $("#form-course");
  state.editingCourseId = course?.id ?? null;
  $("#dlg-course-title").textContent = course ? "Edit course" : "New course";
  f.code.value = course?.code ?? ""; f.title.value = course?.title ?? "";
  f.lecturer.value = course?.lecturer ?? ""; f.semester.value = course?.semester ?? "";
  $("#course-error").hidden = true;
  $("#dlg-course").showModal();
  f.code.focus();
}

$("#form-course").addEventListener("submit", async e => {
  e.preventDefault();
  const f = e.target;
  if (!f.code.value.trim() || !f.title.value.trim()) return showFormError("#course-error", "Course code and title are required.");
  const body = { code: f.code.value, title: f.title.value, lecturer: f.lecturer.value, semester: f.semester.value };
  try {
    if (state.editingCourseId) await api(`/courses/${state.editingCourseId}`, { method: "PUT", body });
    else await api("/courses", { method: "POST", body });
    $("#dlg-course").close();
    toast(state.editingCourseId ? "Course updated" : "Course added");
    refresh();
  } catch (err) { showFormError("#course-error", err.message); }
});

function showFormError(sel, message) {
  const el = $(sel);
  el.innerHTML = `${icon("i-alert")}<span>${esc(message)}</span>`;
  el.hidden = false;
}

function confirmDialog(title, text) {
  const dlg = $("#dlg-confirm");
  $("#confirm-title").textContent = title;
  $("#confirm-text").textContent = text;
  dlg.returnValue = "";
  dlg.showModal();
  $("#confirm-ok").focus();
  return new Promise(resolve => dlg.addEventListener("close", () => resolve(dlg.returnValue === "ok"), { once: true }));
}

$$("[data-close]").forEach(b => b.addEventListener("click", () => b.closest("dialog").close()));
$$("dialog").forEach(d => d.addEventListener("click", e => { if (e.target === d) d.close(); }));

// =====================================================================
// Global actions, theme and shortcuts
// =====================================================================
document.addEventListener("click", e => {
  const a = e.target.closest("[data-action]");
  if (!a) return;
  if (a.dataset.action === "new-resource") openResourceDialog();
  if (a.dataset.action === "new-course") openCourseDialog();
  if (a.dataset.action === "clear-filters") clearFilters();
});

$("#btn-theme").addEventListener("click", () => {
  const next = document.documentElement.dataset.theme === "dark" ? "light" : "dark";
  document.documentElement.dataset.theme = next;
  localStorage.setItem("theme", next);
});

document.addEventListener("keydown", e => {
  if (e.metaKey || e.ctrlKey || e.altKey || document.querySelector("dialog[open]")) return;
  if (e.target.closest("input, textarea, select")) {
    if (e.key === "Escape" && e.target.id === "f-q") e.target.blur();
    return;
  }
  if (e.key === "/") {
    e.preventDefault();
    if (location.hash !== "#resources") location.hash = "resources";
    setTimeout(() => $("#f-q").focus(), 0);
  } else if (e.key === "n") {
    e.preventDefault();
    openResourceDialog();
  }
});

// =====================================================================
// Start
// =====================================================================
function hideSplash() {
  const splash = $("#splash");
  if (!splash || splash.classList.contains("out")) return;
  splash.classList.add("out");
  splash.addEventListener("transitionend", () => splash.remove(), { once: true });
}

window.addEventListener("hashchange", route);
// Long enough to avoid a flash on fast loads; the timeout covers a slow or sleeping server.
Promise.all([route(), new Promise(r => setTimeout(r, 900))]).finally(hideSplash);
setTimeout(hideSplash, 8000);
