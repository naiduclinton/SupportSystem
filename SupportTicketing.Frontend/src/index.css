@tailwind base;
@tailwind components;
@tailwind utilities;

@import url('https://fonts.googleapis.com/css2?family=DM+Sans:ital,opsz,wght@0,9..40,300;0,9..40,400;0,9..40,500;0,9..40,600;1,9..40,400&family=Syne:wght@600;700&family=JetBrains+Mono:wght@400;500&display=swap');

/* ── Design tokens ──────────────────────────────────────────────────────── */
:root {
  --surface-0:    #ffffff;
  --surface-1:    #f8f8f7;
  --surface-2:    #f1f0ef;
  --surface-3:    #e8e7e5;
  --border:       rgba(0,0,0,0.08);
  --accent:       #1a56db;
  --accent-muted: rgba(26,86,219,0.12);
  --accent-fg:    #1a56db;
  --ink:          #111110;
  --ink-2:        #6b6b6a;
  --ink-3:        #a0a09e;
  color-scheme: light;
}

.dark {
  --surface-0:    #141414;
  --surface-1:    #1c1c1c;
  --surface-2:    #242424;
  --surface-3:    #2e2e2e;
  --border:       rgba(255,255,255,0.07);
  --accent:       #4f8ef7;
  --accent-muted: rgba(79,142,247,0.14);
  --accent-fg:    #7aacff;
  --ink:          #efefef;
  --ink-2:        #9a9a98;
  --ink-3:        #636360;
  color-scheme: dark;
}

/* ── Base ───────────────────────────────────────────────────────────────── */
* { box-sizing: border-box; }

body {
  margin: 0;
  font-family: 'DM Sans', sans-serif;
  background: var(--surface-1);
  color: var(--ink);
  font-size: 14px;
  line-height: 1.6;
  -webkit-font-smoothing: antialiased;
}

/* ── Scrollbar ──────────────────────────────────────────────────────────── */
::-webkit-scrollbar { width: 5px; height: 5px; }
::-webkit-scrollbar-track { background: transparent; }
::-webkit-scrollbar-thumb { background: var(--surface-3); border-radius: 10px; }

/* ── Selection ──────────────────────────────────────────────────────────── */
::selection { background: var(--accent-muted); color: var(--ink); }

/* ── Focus ──────────────────────────────────────────────────────────────── */
:focus-visible {
  outline: 2px solid var(--accent);
  outline-offset: 2px;
  border-radius: 4px;
}

/* ── Layout atoms ───────────────────────────────────────────────────────── */
.app-shell {
  display: grid;
  grid-template-columns: 220px 1fr;
  grid-template-rows: 100vh;
  overflow: hidden;
}

/* ── Card ───────────────────────────────────────────────────────────────── */
.card {
  background: var(--surface-0);
  border: 1px solid var(--border);
  border-radius: 12px;
}

/* ── Input base ─────────────────────────────────────────────────────────── */
.input-base {
  background: var(--surface-1);
  border: 1px solid var(--border);
  border-radius: 8px;
  color: var(--ink);
  font-family: 'DM Sans', sans-serif;
  font-size: 14px;
  padding: 8px 12px;
  transition: border-color 0.15s, box-shadow 0.15s;
  width: 100%;
}
.input-base:focus {
  outline: none;
  border-color: var(--accent);
  box-shadow: 0 0 0 3px var(--accent-muted);
}
.input-base::placeholder { color: var(--ink-3); }

/* ── Button atoms ───────────────────────────────────────────────────────── */
.btn {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 7px 14px;
  border-radius: 8px;
  font-family: 'DM Sans', sans-serif;
  font-size: 14px;
  font-weight: 500;
  border: 1px solid var(--border);
  background: var(--surface-0);
  color: var(--ink);
  cursor: pointer;
  transition: background 0.12s, border-color 0.12s, transform 0.08s;
  white-space: nowrap;
}
.btn:hover  { background: var(--surface-2); }
.btn:active { transform: scale(0.98); }

.btn-primary {
  background: var(--accent);
  border-color: var(--accent);
  color: #fff;
}
.btn-primary:hover { opacity: 0.9; background: var(--accent); }

.btn-ghost {
  border-color: transparent;
  background: transparent;
}
.btn-ghost:hover { background: var(--surface-2); }

.btn-danger {
  background: #fef2f2;
  border-color: #fca5a5;
  color: #dc2626;
}
.dark .btn-danger { background: rgba(220,38,38,0.12); border-color: rgba(220,38,38,0.3); color: #f87171; }

/* ── Badge ──────────────────────────────────────────────────────────────── */
.badge {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 2px 8px;
  border-radius: 20px;
  font-size: 11px;
  font-weight: 500;
  letter-spacing: 0.01em;
}

/* ── Table ──────────────────────────────────────────────────────────────── */
.data-table { width: 100%; border-collapse: collapse; }
.data-table th {
  text-align: left;
  font-size: 11px;
  font-weight: 500;
  color: var(--ink-3);
  text-transform: uppercase;
  letter-spacing: 0.06em;
  padding: 10px 16px;
  border-bottom: 1px solid var(--border);
  white-space: nowrap;
}
.data-table td {
  padding: 12px 16px;
  border-bottom: 1px solid var(--border);
  vertical-align: middle;
}
.data-table tr:last-child td { border-bottom: none; }
.data-table tbody tr {
  cursor: pointer;
  transition: background 0.1s;
}
.data-table tbody tr:hover { background: var(--surface-1); }

/* ── SLA bar ────────────────────────────────────────────────────────────── */
.sla-track {
  height: 4px;
  border-radius: 2px;
  background: var(--surface-3);
  overflow: hidden;
  width: 64px;
}
.sla-fill { height: 100%; border-radius: 2px; transition: width 0.3s; }

/* ── Sidebar nav ─────────────────────────────────────────────────────────── */
.nav-link {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 12px;
  border-radius: 8px;
  font-size: 13.5px;
  color: var(--ink-2);
  cursor: pointer;
  transition: background 0.1s, color 0.1s;
  text-decoration: none;
  position: relative;
}
.nav-link:hover { background: var(--surface-2); color: var(--ink); }
.nav-link.active {
  background: var(--accent-muted);
  color: var(--accent-fg);
  font-weight: 500;
}

/* ── Drawer ──────────────────────────────────────────────────────────────── */
.drawer-backdrop {
  position: fixed; inset: 0;
  background: rgba(0,0,0,0.3);
  backdrop-filter: blur(2px);
  z-index: 40;
  animation: fadeIn 0.15s ease-out;
}
.drawer {
  position: fixed; right: 0; top: 0; bottom: 0;
  width: 480px;
  background: var(--surface-0);
  border-left: 1px solid var(--border);
  z-index: 50;
  overflow-y: auto;
  animation: slideIn 0.2s ease-out;
}

/* ── Modal ───────────────────────────────────────────────────────────────── */
.modal-backdrop {
  position: fixed; inset: 0;
  background: rgba(0,0,0,0.35);
  backdrop-filter: blur(3px);
  z-index: 40;
  display: flex; align-items: center; justify-content: center;
  animation: fadeIn 0.15s ease-out;
}
.modal {
  background: var(--surface-0);
  border: 1px solid var(--border);
  border-radius: 16px;
  width: 520px;
  max-height: 90vh;
  overflow-y: auto;
  animation: fadeIn 0.15s ease-out;
  box-shadow: 0 20px 60px rgba(0,0,0,0.2);
}

/* ── Stat card ───────────────────────────────────────────────────────────── */
.stat-card {
  background: var(--surface-0);
  border: 1px solid var(--border);
  border-radius: 12px;
  padding: 18px 20px;
}

/* ── Avatar ──────────────────────────────────────────────────────────────── */
.avatar {
  display: inline-flex; align-items: center; justify-content: center;
  border-radius: 50%;
  font-weight: 600;
  font-size: 11px;
  letter-spacing: 0.03em;
  flex-shrink: 0;
}

/* ── Utilities ───────────────────────────────────────────────────────────── */
.truncate-1 {
  overflow: hidden;
  white-space: nowrap;
  text-overflow: ellipsis;
}
.font-display { font-family: 'Syne', sans-serif; }
.font-mono    { font-family: 'JetBrains Mono', monospace; }

/* ── Tab bar ─────────────────────────────────────────────────────────────── */
.tab-bar { display: flex; gap: 2px; border-bottom: 1px solid var(--border); }
.tab {
  padding: 8px 14px;
  font-size: 13px;
  color: var(--ink-2);
  cursor: pointer;
  border-bottom: 2px solid transparent;
  margin-bottom: -1px;
  transition: color 0.12s, border-color 0.12s;
  white-space: nowrap;
}
.tab:hover { color: var(--ink); }
.tab.active { color: var(--accent-fg); border-bottom-color: var(--accent); font-weight: 500; }

/* ── Toast ───────────────────────────────────────────────────────────────── */
.toast-container {
  position: fixed; bottom: 24px; right: 24px; z-index: 100;
  display: flex; flex-direction: column; gap: 8px;
}
.toast {
  background: var(--surface-0);
  border: 1px solid var(--border);
  border-radius: 10px;
  padding: 12px 16px;
  font-size: 13px;
  box-shadow: 0 4px 16px rgba(0,0,0,0.12);
  display: flex; align-items: center; gap: 10px;
  animation: fadeIn 0.2s ease-out;
  max-width: 320px;
}
