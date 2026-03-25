<template>
  <div class="app">
    <header>
      <h1>✂️ URL Shortener</h1>
      <p>Paste a long URL and get a short link instantly</p>
    </header>

    <main>
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
            {{ loading ? 'Shortening...' : 'Shorten' }}
          </button>
        </div>
        <p v-if="error" class="error">{{ error }}</p>
        <div v-if="result" class="result">
          <p>Your short link:</p>
          <div class="link-row">
            <a :href="result.shortLink" target="_blank">{{ result.shortLink }}</a>
            <button class="copy-btn" @click="copyLink">📋 Copy</button>
          </div>
          <p v-if="copied" class="copied-msg">Copied!</p>
        </div>
      </section>

      <section class="card">
        <div class="table-header">
          <h2>All Shortened URLs</h2>
          <button class="refresh-btn" @click="loadUrls">🔄 Refresh</button>
        </div>
        <div v-if="urls.length === 0" class="empty">No URLs yet.</div>
        <table v-else>
          <thead>
            <tr>
              <th>Short Code</th>
              <th>Original URL</th>
              <th>Clicks</th>
              <th>Created</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="url in urls" :key="url.id">
              <td><a :href="`${apiBase}/r/${url.code}`" target="_blank">{{ url.code }}</a></td>
              <td class="truncate">{{ url.originalUrl }}</td>
              <td>{{ url.clickCount }}</td>
              <td>{{ formatDate(url.createdAt) }}</td>
            </tr>
          </tbody>
        </table>
      </section>
    </main>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'

const apiBase = import.meta.env.VITE_API_URL || 'http://localhost:5000'
const inputUrl = ref('')
const loading = ref(false)
const error = ref('')
const result = ref(null)
const copied = ref(false)
const urls = ref([])

async function shortenUrl() {
  error.value = ''
  result.value = null
  if (!inputUrl.value.trim()) { error.value = 'Please enter a URL.'; return }
  loading.value = true
  try {
    const res = await fetch(`${apiBase}/api/url`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ originalUrl: inputUrl.value.trim() })
    })
    const data = await res.json()
    if (!res.ok) { error.value = data.error || 'Something went wrong.' }
    else { result.value = data; inputUrl.value = ''; await loadUrls() }
  } catch { error.value = 'Cannot connect to the server.' }
  finally { loading.value = false }
}

async function loadUrls() {
  try {
    const res = await fetch(`${apiBase}/api/url`)
    urls.value = await res.json()
  } catch { /* silent */ }
}

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
.app { max-width: 820px; margin: 0 auto; padding: 2rem 1rem; }
header { text-align: center; margin-bottom: 2rem; }
header h1 { font-size: 2rem; color: #2d3748; }
header p { color: #718096; margin-top: 0.4rem; }
.card { background: white; border-radius: 12px; padding: 1.5rem;
        box-shadow: 0 2px 8px rgba(0,0,0,0.08); margin-bottom: 1.5rem; }
.card h2 { font-size: 1.1rem; margin-bottom: 1rem; color: #2d3748; }
.input-row { display: flex; gap: 0.5rem; }
.input-row input { flex: 1; padding: 0.75rem 1rem; border: 1px solid #cbd5e0;
                   border-radius: 8px; font-size: 0.95rem; }
.input-row input:focus { outline: none; border-color: #667eea; }
.input-row button { padding: 0.75rem 1.25rem; background: #667eea; color: white;
                    border: none; border-radius: 8px; cursor: pointer; font-weight: 600; }
.input-row button:hover:not(:disabled) { background: #5a67d8; }
.input-row button:disabled { opacity: 0.6; cursor: not-allowed; }
.error { color: #e53e3e; margin-top: 0.75rem; font-size: 0.9rem; }
.result { margin-top: 1rem; padding: 1rem; background: #f0fff4;
          border-radius: 8px; border: 1px solid #9ae6b4; }
.result p { font-size: 0.85rem; color: #276749; margin-bottom: 0.4rem; }
.link-row { display: flex; align-items: center; gap: 0.75rem; flex-wrap: wrap; }
.link-row a { color: #2b6cb0; font-weight: 600; word-break: break-all; }
.copy-btn { padding: 0.3rem 0.75rem; background: white; border: 1px solid #9ae6b4;
            border-radius: 6px; cursor: pointer; font-size: 0.8rem; }
.copied-msg { color: #276749; font-size: 0.8rem; margin-top: 0.3rem; }
.table-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; }
.refresh-btn { padding: 0.4rem 0.8rem; background: #edf2f7; border: none;
               border-radius: 6px; cursor: pointer; font-size: 0.85rem; }
.refresh-btn:hover { background: #e2e8f0; }
.empty { color: #a0aec0; text-align: center; padding: 1.5rem; }
table { width: 100%; border-collapse: collapse; font-size: 0.9rem; }
th { background: #f7fafc; text-align: left; padding: 0.6rem 0.75rem;
     border-bottom: 2px solid #e2e8f0; color: #4a5568; }
td { padding: 0.6rem 0.75rem; border-bottom: 1px solid #e2e8f0; vertical-align: middle; }
tr:hover td { background: #f7fafc; }
td a { color: #2b6cb0; font-weight: 600; text-decoration: none; }
td a:hover { text-decoration: underline; }
.truncate { max-width: 280px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
</style>