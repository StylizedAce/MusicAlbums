const state = {
  provider: "",
  user: "demo",
  audio: null,
  playingButton: null
};

const els = {
  provider: document.getElementById("provider"),
  user: document.getElementById("user"),
  searchForm: document.getElementById("search-form"),
  query: document.getElementById("query"),
  searchStatus: document.getElementById("search-status"),
  results: document.getElementById("results"),
  libraryStatus: document.getElementById("library-status"),
  library: document.getElementById("library"),
  refresh: document.getElementById("refresh"),
  toast: document.getElementById("toast"),
  healthDot: document.getElementById("health-dot")
};

async function request(path, options = {}, allowNotFound = false) {
  const response = await fetch(path, options);
  if (allowNotFound && response.status === 404) return null;
  if (!response.ok) {
    let detail = `HTTP ${response.status}`;
    try {
      const problem = await response.json();
      detail = problem.detail || problem.title || detail;
    } catch { /* not json */ }
    const error = new Error(detail);
    error.status = response.status;
    throw error;
  }
  if (response.status === 204) return null;
  return response.json();
}

function escapeHtml(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

function formatDuration(seconds) {
  if (!seconds && seconds !== 0) return "";
  const minutes = Math.floor(seconds / 60);
  const rest = String(seconds % 60).padStart(2, "0");
  return `${minutes}:${rest}`;
}

function coverHtml(album) {
  return album.coverImageUrl
    ? `<img src="${escapeHtml(album.coverImageUrl)}" alt="" loading="lazy" />`
    : `<div class="cover">&#9835;</div>`;
}

function albumMeta(album) {
  const parts = [escapeHtml(album.artist)];
  if (album.releaseDate) parts.push(String(album.releaseDate).slice(0, 4));
  if (album.trackCount) parts.push(`${album.trackCount} tracks`);
  return parts.join(" &middot; ");
}

function externalLink(album) {
  return album.externalUrl
    ? `<a class="external" href="${escapeHtml(album.externalUrl)}" target="_blank" rel="noopener">Open on ${escapeHtml(album.provider)}</a>`
    : "";
}

function trackList(tracks) {
  if (!tracks || tracks.length === 0) return "";
  const items = tracks.map(track => `
    <li>
      <span class="pos">${track.position}.</span>
      <span class="title">${escapeHtml(track.title)}</span>
      ${track.previewUrl ? `<button class="play" data-preview="${escapeHtml(track.previewUrl)}">&#9654;</button>` : ""}
      <span class="dur">${formatDuration(track.durationSeconds)}</span>
    </li>`).join("");
  return `<details><summary>${tracks.length} tracks</summary><ul class="tracks">${items}</ul></details>`;
}

function renderResults(albums) {
  els.results.innerHTML = albums.map(album => `
    <article class="card">
      ${coverHtml(album)}
      <h3 title="${escapeHtml(album.title)}">${escapeHtml(album.title)}</h3>
      <div class="meta">${albumMeta(album)}</div>
      ${externalLink(album)}
      <div class="actions">
        <button class="save" data-provider="${escapeHtml(album.provider)}" data-id="${escapeHtml(album.providerAlbumId)}">Save</button>
      </div>
    </article>`).join("");
}

function renderLibrary(albums) {
  els.library.innerHTML = albums.map(album => `
    <article class="card">
      ${coverHtml(album)}
      <h3 title="${escapeHtml(album.title)}">${escapeHtml(album.title)}</h3>
      <div class="meta">${albumMeta(album)}</div>
      ${externalLink(album)}
      ${trackList(album.tracks)}
      <div class="actions">
        <button class="delete" data-id="${escapeHtml(album.id)}">Remove</button>
      </div>
    </article>`).join("");
}

function setStatus(element, text, isError = false) {
  element.textContent = text ?? "";
  element.classList.toggle("error", isError);
}

let toastTimer = null;
function toast(message) {
  els.toast.textContent = message;
  els.toast.hidden = false;
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => { els.toast.hidden = true; }, 4200);
}

async function loadProviders() {
  const providers = await request("/api/providers");
  els.provider.innerHTML = providers
    .map(item => `<option value="${escapeHtml(item.name)}"${item.isDefault ? " selected" : ""}>${escapeHtml(item.name)}${item.isDefault ? " (default)" : ""}</option>`)
    .join("");
  state.provider = els.provider.value;
}

async function search(text) {
  setStatus(els.searchStatus, "Searching\u2026");
  try {
    const data = await request(`/api/albums/search?q=${encodeURIComponent(text)}&provider=${encodeURIComponent(state.provider)}&limit=12`);
    renderResults(data.albums);
    setStatus(els.searchStatus, `${data.total} results from ${state.provider}`);
  } catch (error) {
    setStatus(els.searchStatus, error.message, true);
  }
}

async function saveAlbum(provider, providerAlbumId, button) {
  button.disabled = true;
  try {
    await request(`/api/users/${encodeURIComponent(state.user)}/library`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ provider, providerAlbumId })
    });
    toast("Saved to library");
    await loadLibrary();
  } catch (error) {
    toast(error.status === 409 ? "Already in the library" : error.message);
    button.disabled = false;
  }
}

async function removeAlbum(albumId) {
  try {
    await request(`/api/users/${encodeURIComponent(state.user)}/library/${albumId}`, { method: "DELETE" });
    stopPreview();
    await loadLibrary();
  } catch (error) {
    toast(error.message);
  }
}

async function loadLibrary() {
  setStatus(els.libraryStatus, "Loading\u2026");
  try {
    const albums = await request(`/api/users/${encodeURIComponent(state.user)}/library`, {}, true) ?? [];
    renderLibrary(albums);
    setStatus(els.libraryStatus, albums.length === 0 ? "Library is empty \u2014 search and save an album." : `${albums.length} album(s) for ${state.user}`);
  } catch (error) {
    setStatus(els.libraryStatus, error.message, true);
  }
}

function stopPreview() {
  if (state.audio) {
    state.audio.pause();
    state.audio = null;
  }
  if (state.playingButton) {
    state.playingButton.innerHTML = "&#9654;";
    state.playingButton = null;
  }
}

function togglePreview(button) {
  const url = button.dataset.preview;
  if (state.playingButton === button) {
    stopPreview();
    return;
  }
  stopPreview();
  state.audio = new Audio(url);
  state.playingButton = button;
  state.audio.addEventListener("ended", stopPreview);
  state.audio.addEventListener("error", () => { stopPreview(); toast("Preview unavailable (signed URL may have expired)"); });
  state.audio.play()
    .then(() => { button.innerHTML = "&#10074;&#10074;"; })
    .catch(() => { stopPreview(); toast("Preview could not be played"); });
}

async function checkHealth() {
  try {
    const response = await fetch("/health/ready");
    els.healthDot.className = `dot ${response.ok ? "ok" : "bad"}`;
    els.healthDot.title = response.ok ? "API healthy" : `API unhealthy (${response.status})`;
  } catch {
    els.healthDot.className = "dot bad";
    els.healthDot.title = "API unreachable";
  }
}

els.searchForm.addEventListener("submit", event => {
  event.preventDefault();
  const text = els.query.value.trim();
  if (text) search(text);
});

els.provider.addEventListener("change", () => { state.provider = els.provider.value; });

els.user.addEventListener("change", () => {
  state.user = els.user.value.trim() || "demo";
  els.user.value = state.user;
  stopPreview();
  loadLibrary();
});

els.refresh.addEventListener("click", loadLibrary);

els.results.addEventListener("click", event => {
  const button = event.target.closest("button.save");
  if (button) saveAlbum(button.dataset.provider, button.dataset.id, button);
});

els.library.addEventListener("click", event => {
  const play = event.target.closest("button.play");
  if (play) {
    togglePreview(play);
    return;
  }
  const remove = event.target.closest("button.delete");
  if (remove) removeAlbum(remove.dataset.id);
});

(async function init() {
  await checkHealth();
  setInterval(checkHealth, 15000);
  try {
    await loadProviders();
  } catch (error) {
    toast(`Could not load providers: ${error.message}`);
  }
  await loadLibrary();
})();
