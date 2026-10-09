/* ============================================================
   EcomAdmin — JavaScript Helpers
   ============================================================ */

// --- Live Clock ---
function updateClock() {
    const el = document.getElementById('topbarTime');
    if (!el) return;
    const now = new Date();
    el.textContent = now.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' });
}
setInterval(updateClock, 1000);
updateClock();

// --- Sidebar Toggle ---
const sidebar = document.getElementById('sidebar');
const mainWrapper = document.getElementById('mainWrapper');
const sidebarToggle = document.getElementById('sidebarToggle');
if (sidebarToggle) {
    sidebarToggle.addEventListener('click', () => {
        sidebar.classList.toggle('collapsed');
        mainWrapper.classList.toggle('expanded');
    });
}

// --- Toast Notifications ---
function showToast(message, type = 'info', duration = 4000) {
    const container = document.getElementById('toastContainer');
    if (!container) return;

    const icons = {
        success: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polyline points="20 6 9 17 4 12"/></svg>`,
        error:   `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><circle cx="12" cy="12" r="10"/><line x1="15" y1="9" x2="9" y2="15"/><line x1="9" y1="9" x2="15" y2="15"/></svg>`,
        info:    `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/></svg>`
    };

    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    toast.innerHTML = `<span class="toast-icon">${icons[type] || icons.info}</span><span class="toast-message">${message}</span>`;
    container.appendChild(toast);

    toast.addEventListener('click', () => dismissToast(toast));

    setTimeout(() => dismissToast(toast), duration);
}
function dismissToast(toast) {
    toast.classList.add('exit');
    setTimeout(() => toast.remove(), 300);
}

// --- JWT Token Management ---
function saveToken() {
    const token = document.getElementById('globalToken')?.value?.trim();
    if (!token) { showToast('Token field is empty.', 'error'); return; }
    fetch('/Admin/SaveToken', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': getAntiForgeryToken() },
        body: JSON.stringify({ token })
    }).then(r => {
        if (r.ok) {
            showToast('Bearer token saved to session.', 'success');
        } else {
            showToast('Failed to save token.', 'error');
        }
    }).catch(() => showToast('Network error saving token.', 'error'));
}

function getAntiForgeryToken() {
    const el = document.querySelector('input[name="__RequestVerificationToken"]');
    return el ? el.value : '';
}

// --- Copy to Clipboard ---
function copyToClipboard(text, label = 'Value') {
    navigator.clipboard.writeText(text)
        .then(() => showToast(`${label} copied to clipboard.`, 'success'))
        .catch(() => showToast('Failed to copy.', 'error'));
}

function copyElement(id, label) {
    const el = document.getElementById(id);
    if (el) copyToClipboard(el.textContent || el.value, label);
}

// --- Tab System ---
function initTabs(tabGroupId) {
    const group = document.getElementById(tabGroupId);
    if (!group) return;
    const buttons = group.querySelectorAll('.tab-btn');
    buttons.forEach(btn => {
        btn.addEventListener('click', () => {
            buttons.forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            const target = btn.dataset.tab;
            group.querySelectorAll('.tab-panel').forEach(p => {
                p.classList.toggle('active', p.id === target);
            });
        });
    });
    // Activate first tab
    if (buttons[0]) buttons[0].click();
}

// --- Button Loading State ---
function setLoading(btnId, loading) {
    const btn = document.getElementById(btnId);
    if (!btn) return;
    btn.classList.toggle('loading', loading);
    btn.disabled = loading;
}

// --- Fetch with JSON ---
async function apiCall(url, options = {}) {
    const token = document.getElementById('globalToken')?.value?.trim();
    const headers = { 'Content-Type': 'application/json', ...options.headers };
    if (token) headers['Authorization'] = `Bearer ${token}`;
    const response = await fetch(url, { ...options, headers });
    const text = await response.text();
    let data;
    try { data = JSON.parse(text); } catch { data = text; }
    return { ok: response.ok, status: response.status, data };
}

// --- Display Result in a result box ---
function displayResult(boxId, result, isOk) {
    const box = document.getElementById(boxId);
    if (!box) return;
    box.className = 'result-box ' + (isOk ? 'success' : 'error');
    box.textContent = typeof result === 'object'
        ? JSON.stringify(result, null, 2)
        : String(result);
}

function showLoading(boxId, message = 'Calling API...') {
    const box = document.getElementById(boxId);
    if (!box) return;
    box.className = 'result-box loading-state';
    box.innerHTML = `<svg style="width:18px;height:18px;animation:spin 0.7s linear infinite;flex-shrink:0;" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0z" stroke-opacity="0.2"/><path d="M21 12a9 9 0 0 0-9-9"/></svg>${message}`;
}

// --- Gateway Health Check ---
async function checkGatewayHealth() {
    const dot = document.getElementById('gatewayStatusDot');
    try {
        const res = await fetch('/swagger/v1/swagger.json', { method: 'HEAD' });
        if (dot) dot.style.background = res.ok ? 'var(--success)' : 'var(--danger)';
    } catch {
        if (dot) dot.style.background = 'var(--danger)';
    }
}
checkGatewayHealth();
setInterval(checkGatewayHealth, 30000);

// --- Render a JSON result as a pretty table if it's an array ---
function renderTable(containerId, data, columns) {
    const container = document.getElementById(containerId);
    if (!container) return;
    if (!Array.isArray(data) || data.length === 0) {
        container.innerHTML = '<p style="color:var(--text-muted);font-style:italic;padding:16px;">No data returned.</p>';
        return;
    }
    const cols = columns || Object.keys(data[0]);
    let html = '<table class="data-table"><thead><tr>';
    cols.forEach(c => html += `<th>${c}</th>`);
    html += '</tr></thead><tbody>';
    data.forEach(row => {
        html += '<tr>';
        cols.forEach(c => html += `<td>${row[c] ?? '—'}</td>`);
        html += '</tr>';
    });
    html += '</tbody></table>';
    container.innerHTML = html;
}

// -- Token auto-fill from saved session (set by Razor page) --
window.addEventListener('DOMContentLoaded', () => {
    // Init any tab groups found on the page
    document.querySelectorAll('[data-tabs]').forEach(el => initTabs(el.id));
});
