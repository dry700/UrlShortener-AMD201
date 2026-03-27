<template>
  <div class="app">
    <header>
      <h1>✂️ URL Shortener</h1>
      <p>Paste a long URL and get a short link instantly</p>
    </header>

    <main>
      <!-- CREATE form -->
      <section class="card">
        <h2>Shorten a URL</h2>
        <div class="input-row">
          <input
            v-model="inputUrl"
            type="text"
            placeholder="https://example.com/very/long/url"
            @keyup.enter="shortenUrl"
          />
          <button @click="shortenUrl" :disabled="loading">
            {{ loading ? 'Shortening…' : 'Shorten' }}
          </button>
        </div>
        <p v-if="createError" class="msg error">{{ createError }}</p>
        <div v-if="result" class="result">
          <p>Your short link:</p>
          <div class="link-row">
            <a :href="result.shortLink" target="_blank">{{ result.shortLink }}</a>
            <button class="copy-btn" @click="copyLink">📋 Copy</button>
          </div>
          <p v-if="copied" class="msg success">Copied!</p>
        </div>
      </section>

      <!-- CRUD table -->
      <section class="card">
        <div class="table-header">
          <h2>All Shortened URLs</h2>
          <button class="icon-btn" @click="loadUrls" title="Refresh">🔄 Refresh</button>
        </div>

        <div v-if="urls.length === 0" class="empty">No URLs yet.</div>

        <table v-else>
          <thead>
            <tr>
              <th>Short code</th>
              <th>Original URL</th>
              <th>Clicks</th>
              <th>Created</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="url in urls" :key="url.id">

              <!-- Inline edit mode -->
              <template v-if="editingCode === url.code">
                <td>
                  <a :href="`${apiBase}/r/${url.code}`" target="_blank">{{ url.code }}</a>
                </td>
                <td colspan="3">
                  <input
                    class="edit-input"
                    v-model="editValue"
                    @keyup.enter="saveEdit(url.code)"
                    @keyup.escape="cancelEdit"
                    placeholder="New destination URL"
                  />
                  <p v-if="editError" class="msg error small">{{ editError }}</p>
                </td>
                <td>
                  <div class="action-row">
                    <button class="btn-save"   @click="saveEdit(url.code)">✓ Save</button>
                    <button class="btn-cancel" @click="cancelEdit">✕</button>
                  </div>
                </td>
              </template>

              <!-- Normal read mode -->
              <template v-else>
                <td>
                  <a :href="`${apiBase}/r/${url.code}`" target="_blank">{{ url.code }}</a>
                </td>
                <td class="truncate">{{ url.originalUrl }}</td>
                <td>{{ url.clickCount }}</td>
                <td>{{ formatDate(url.createdAt) }}</td>
                <td>
                  <div class="action-row">
                    <button class="btn-edit"   @click="startEdit(url)">✏️ Edit</button>
                    <button class="btn-delete" @click="deleteUrl(url.code)">🗑️ Delete</button>
                  </div>
                </td>
              </template>

            </tr>
          </tbody>
        </table>
      </section>
    </main>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'

const apiBase  = import.meta.env.VITE_API_URL || 'http://localhost:5000'
const inputUrl = ref('')
const loading  = ref(false)
const result   = ref(null)
const copied   = ref(false)
const urls     = ref([])

const createError = ref('')
const editingCode = ref('')
const editValue   = ref('')
const editError   = ref('')

// ── CREATE ───────────────────────────────────────────────────────────────────
async function shortenUrl() {
  createError.value = ''
  result.value      = null
  if (!inputUrl.value.trim()) { createError.value = 'Please enter a URL.'; return }
  loading.value = true
  try {
    const res  = await fetch(`${apiBase}/api/url`, {
      method:  'POST',
      headers: { 'Content-Type': 'application/json' },
      body:    JSON.stringify({ originalUrl: inputUrl.value.trim() })
    })
    const data = await res.json()
    if (!res.ok) { createError.value = data.error || 'Something went wrong.' }
    else         { result.value = data; inputUrl.value = ''; await loadUrls() }
  } catch { createError.value = 'Cannot connect to the server.' }
  finally { loading.value = false }
}

// ── READ ─────────────────────────────────────────────────────────────────────
async function loadUrls() {
  try {
    const res = await fetch(`${apiBase}/api/url`)
    urls.value = await res.json()
  } catch { /* silent */ }
}

// ── UPDATE ───────────────────────────────────────────────────────────────────
function startEdit(url) {
  editingCode.value = url.code
  editValue.value   = url.originalUrl
  editError.value   = ''
}

function cancelEdit() {
  editingCode.value = ''
  editValue.value   = ''
  editError.value   = ''
}

async function saveEdit(code) {
  editError.value = ''
  if (!editValue.value.trim()) { editError.value = 'URL is required.'; return }
  try {
    const res  = await fetch(`${apiBase}/api/url/${code}`, {
      method:  'PUT',
      headers: { 'Content-Type': 'application/json' },
      body:    JSON.stringify({ originalUrl: editValue.value.trim() })
    })
    const data = await res.json()
    if (!res.ok) { editError.value = data.error || 'Update failed.'; return }
    cancelEdit()
    await loadUrls()
  } catch { editError.value = 'Cannot connect to the server.' }
}

// ── DELETE ───────────────────────────────────────────────────────────────────
async function deleteUrl(code) {
  if (!confirm(`Delete short code "${code}"?`)) return
  try {
    await fetch(`${apiBase}/api/url/${code}`, { method: 'DELETE' })
    await loadUrls()
  } catch { /* silent */ }
}

// ── HELPERS ───────────────────────────────────────────────────────────────────
function copyLink() {
  navigator.clipboard.writeText(result.value.shortLink)
  copied.value = true
  setTimeout(() => (copied.value = false), 2000)
}

function formatDate(d) { return new Date(d).toLocaleString() }

onMounted(loadUrls)
</script>

<style>
* { box-sizing: border-box; margin: 0; padding: 0; }
body { font-family: 'Segoe UI', sans-serif; background: #f0f4f8; color: #333; }
.app  { max-width: 900px; margin: 0 auto; padding: 2rem 1rem; }

header { text-align: center; margin-bottom: 2rem; }
header h1 { font-size: 2rem; color: #2d3748; }
header p  { color: #718096; margin-top: .4rem; }

.card { background: white; border-radius: 12px; padding: 1.5rem;
        box-shadow: 0 2px 8px rgba(0,0,0,.08); margin-bottom: 1.5rem; }
.card h2 { font-size: 1.1rem; margin-bottom: 1rem; color: #2d3748; }

.input-row { display: flex; gap: .5rem; }
.input-row input { flex: 1; padding: .75rem 1rem; border: 1px solid #cbd5e0;
                   border-radius: 8px; font-size: .95rem; }
.input-row input:focus { outline: none; border-color: #667eea; }
.input-row button { padding: .75rem 1.25rem; background: #667eea; color: white;
                    border: none; border-radius: 8px; cursor: pointer; font-weight: 600; }
.input-row button:hover:not(:disabled) { background: #5a67d8; }
.input-row button:disabled { opacity: .6; cursor: not-allowed; }

.msg { margin-top: .6rem; font-size: .88rem; }
.error   { color: #e53e3e; }
.success { color: #276749; }
.small   { margin-top: .3rem; }

.result { margin-top: 1rem; padding: 1rem; background: #f0fff4;
          border-radius: 8px; border: 1px solid #9ae6b4; }
.result p { font-size: .85rem; color: #276749; margin-bottom: .4rem; }
.link-row { display: flex; align-items: center; gap: .75rem; flex-wrap: wrap; }
.link-row a { color: #2b6cb0; font-weight: 600; word-break: break-all; }
.copy-btn { padding: .3rem .75rem; background: white; border: 1px solid #9ae6b4;
            border-radius: 6px; cursor: pointer; font-size: .8rem; }

.table-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; }
.icon-btn { padding: .4rem .8rem; background: #edf2f7; border: none;
            border-radius: 6px; cursor: pointer; font-size: .85rem; }
.icon-btn:hover { background: #e2e8f0; }

.empty { color: #a0aec0; text-align: center; padding: 1.5rem; }

table { width: 100%; border-collapse: collapse; font-size: .88rem; }
th { background: #f7fafc; text-align: left; padding: .6rem .75rem;
     border-bottom: 2px solid #e2e8f0; color: #4a5568; white-space: nowrap; }
td { padding: .6rem .75rem; border-bottom: 1px solid #e2e8f0; vertical-align: middle; }
tr:hover td { background: #f7fafc; }
td a { color: #2b6cb0; font-weight: 600; text-decoration: none; }
td a:hover { text-decoration: underline; }
.truncate { max-width: 260px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }

.edit-input { width: 100%; padding: .45rem .7rem; border: 1px solid #667eea;
              border-radius: 6px; font-size: .88rem; }
.edit-input:focus { outline: none; border-color: #5a67d8; }

.action-row { display: flex; gap: .4rem; }

.btn-edit   { padding: .3rem .65rem; background: #ebf4ff; color: #2b6cb0;
              border: 1px solid #bee3f8; border-radius: 6px; cursor: pointer; font-size: .8rem; }
.btn-edit:hover { background: #bee3f8; }

.btn-delete { padding: .3rem .65rem; background: #fff5f5; color: #c53030;
              border: 1px solid #fed7d7; border-radius: 6px; cursor: pointer; font-size: .8rem; }
.btn-delete:hover { background: #fed7d7; }

.btn-save   { padding: .3rem .65rem; background: #f0fff4; color: #276749;
              border: 1px solid #9ae6b4; border-radius: 6px; cursor: pointer; font-size: .8rem; font-weight: 600; }
.btn-save:hover { background: #c6f6d5; }

.btn-cancel { padding: .3rem .65rem; background: #f7fafc; color: #718096;
              border: 1px solid #e2e8f0; border-radius: 6px; cursor: pointer; font-size: .8rem; }
.btn-cancel:hover { background: #edf2f7; }
</style>