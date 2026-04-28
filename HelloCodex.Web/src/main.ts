import './style.css';

const app = document.querySelector<HTMLDivElement>('#app');

if (app === null) {
  throw new Error('App root element was not found.');
}

app.innerHTML = `
  <div class="shell">
    <header class="top-bar">
      <a class="brand" href="/" aria-label="HelloCodex home">
        <img src="/favicon.svg" alt="" width="32" height="32" />
        <span>HelloCodex</span>
      </a>
      <nav aria-label="Main navigation">
        <a href="#" aria-current="page">Questionnaires</a>
      </nav>
    </header>
    <main class="content">
      <section class="status-panel" aria-labelledby="status-title">
        <div>
          <p class="eyebrow">API status</p>
          <h1 id="status-title">Questionnaires workspace</h1>
          <p class="summary">
            The frontend calls the local API through Vite's same-origin development proxy.
          </p>
        </div>
        <dl class="status-list">
          <div>
            <dt>GET /api/ping</dt>
            <dd id="ping-response" aria-live="polite">Checking...</dd>
          </div>
        </dl>
      </section>
    </main>
  </div>
`;

const pingResponse = document.querySelector<HTMLElement>('#ping-response');

if (pingResponse === null) {
  throw new Error('Ping response element was not found.');
}

const pingResponseElement = pingResponse;

async function loadPing(): Promise<void> {
  try {
    const response = await fetch('/api/ping');

    if (!response.ok) {
      throw new Error(`Ping request failed with HTTP ${response.status}.`);
    }

    pingResponseElement.textContent = await response.text();
  } catch {
    pingResponseElement.textContent = 'API unavailable';
  }
}

void loadPing();
