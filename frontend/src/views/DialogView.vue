
<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()
interface Session {
  id: number
  title: string
  description: string
  date: string
  createdAt: string
  requirements: string
  status: 'error' | 'success' | 'warning'
  statusText: string
}

const storageKey = computed(() => `analytics-agent-sessions-${authStore.user?.id ?? 'guest'}`)
function loadSessions(): Session[] {
  try {
    const saved = localStorage.getItem(storageKey.value)
    const parsed: unknown = saved ? JSON.parse(saved) : []
    return Array.isArray(parsed) ? parsed.filter((item): item is Session =>
      item !== null && typeof item === 'object' &&
      typeof item.id === 'number' && typeof item.requirements === 'string' &&
      typeof item.createdAt === 'string' && typeof item.title === 'string'
    ) : []
  } catch { return [] }
}
const sessions = ref<Session[]>(loadSessions())
const sidebarOpen = ref(true)
const searchQuery = ref('')
const requirements = ref('')
const activeSessionId = ref<number | null>(null)
const userName = computed(() => authStore.user?.name || 'Имя Фамилия')
const userEmail = computed(() => authStore.user?.email || 'name@mail.ru')
const firstName = computed(() => userName.value.split(' ')[0] || 'Имя')
const initials = computed(() => userName.value.trim().split(/\s+/).slice(0, 2).map(word => word.charAt(0)).join('').toUpperCase())

watch(sessions, value => {
  try { localStorage.setItem(storageKey.value, JSON.stringify(value)) } catch { /* Storage unavailable */ }
}, { deep: true })
watch(storageKey, () => {
  sessions.value = loadSessions()
  activeSessionId.value = null
  requirements.value = ''
})

const filteredSessions = computed(() => sessions.value.filter(session =>
  `${session.title} ${session.description} ${session.requirements}`.toLowerCase().includes(searchQuery.value.toLowerCase().trim())
))
function groupFor(session: Session): 'today' | 'yesterday' | 'earlier' {
  const date = new Date(session.createdAt)
  const now = new Date()
  if (date.toDateString() === now.toDateString()) return 'today'
  const yesterday = new Date(now.getFullYear(), now.getMonth(), now.getDate() - 1)
  if (date.toDateString() === yesterday.toDateString()) return 'yesterday'
  return 'earlier'
}
const todaySessions = computed(() => filteredSessions.value.filter(s => groupFor(s) === 'today'))
const yesterdaySessions = computed(() => filteredSessions.value.filter(s => groupFor(s) === 'yesterday'))
const earlierSessions = computed(() => filteredSessions.value.filter(s => groupFor(s) === 'earlier'))

function newDialog() {
  activeSessionId.value = null
  requirements.value = ''
}
function startDialog() {
  const value = requirements.value.trim()
  if (!value) return
  const now = new Date()
  const session: Session = {
    id: now.getTime(),
    title: value.length > 45 ? `${value.slice(0, 45)}…` : value,
    description: value.length > 85 ? `${value.slice(0, 85)}…` : value,
    requirements: value,
    date: now.toLocaleString('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' }),
    createdAt: now.toISOString(),
    status: 'warning',
    statusText: 'Черновик'
  }
  sessions.value.unshift(session)
  activeSessionId.value = session.id
}
function openSession(session: Session) {
  activeSessionId.value = session.id
  requirements.value = session.requirements
}
function saveChanges() {
  const session = sessions.value.find(s => s.id === activeSessionId.value)
  if (!session || !requirements.value.trim()) return
  const value = requirements.value.trim()
  session.requirements = value
  session.title = value.length > 45 ? `${value.slice(0, 45)}…` : value
  session.description = value.length > 85 ? `${value.slice(0, 85)}…` : value
}
</script>

<template>
  <div class="dialog-page" :class="{ 'sidebar-collapsed': !sidebarOpen }">

    <aside class="dialog-sidebar" :class="{ collapsed: !sidebarOpen }">

      <div class="sidebar-content">
        <button type="button" class="sidebar-close" aria-label="Свернуть историю" @click="sidebarOpen = false">‹</button>
        <h1 class="sidebar-logo">
          Агент-аналитик
        </h1>
        <button type="button" class="new-dialog-button" @click="newDialog">
          <span class="plus-icon">+</span>
          Новый диалог
        </button>

        <input
          class="sidebar-search" v-model="searchQuery"
          type="text"
          placeholder="Поиск по сессиям"
        />

        <div
          v-if="todaySessions.length > 0"
          class="sidebar-group"
        >
          <p class="sidebar-group-title">СЕГОДНЯ</p>

          <div
            v-for="session in todaySessions"
            :key="session.id"
            class="sidebar-session" :class="{ selected: activeSessionId === session.id }" @click="openSession(session)"
          >
            <p class="sidebar-session-title">
              {{ session.title }}
            </p>

            <p class="sidebar-session-description">
              {{ session.description }}
            </p>
          </div>
        </div>

        <div
          v-if="yesterdaySessions.length > 0"
          class="sidebar-group"
        >
          <p class="sidebar-group-title">ВЧЕРА</p>

          <div
            v-for="session in yesterdaySessions"
            :key="session.id"
            class="sidebar-session" :class="{ selected: activeSessionId === session.id }" @click="openSession(session)"
          >
            <p class="sidebar-session-title">
              {{ session.title }}
            </p>

            <p class="sidebar-session-description">
              {{ session.description }}
            </p>
          </div>
        </div>

        <div
          v-if="earlierSessions.length > 0"
          class="sidebar-group"
        >
          <p class="sidebar-group-title">РАНЕЕ</p>

          <div
            v-for="session in earlierSessions"
            :key="session.id"
            class="sidebar-session" :class="{ selected: activeSessionId === session.id }" @click="openSession(session)"
          >
            <p class="sidebar-session-title">
              {{ session.title }}
            </p>

            <p class="sidebar-session-description">
              {{ session.description }}
            </p>
          </div>
        </div>
      </div>

      <div class="sidebar-user">
        <div class="user-avatar">
          {{ initials }}
        </div>

        <div class="user-info">
          <p class="user-name">
            {{ userName }}
          </p>

          <p class="user-email">
            {{ userEmail }}
          </p>
        </div>

        <a href="#" class="logout-link" @click.prevent>
          Выйти
        </a>
      </div>
    </aside>

    <main class="dialog-main">
      <button type="button" class="sidebar-toggle" :aria-label="sidebarOpen ? 'Свернуть историю' : 'Открыть историю'" @click="sidebarOpen = !sidebarOpen">{{ sidebarOpen ? '‹' : '☰' }}</button>
      <div class="dialog-content">

        <!-- Приветствие -->
        <header class="dialog-welcome">
          <h2>
            Здравствуйте, {{ firstName }}
          </h2>

          <p>
            Начните новый диалог или вернитесь к прошлой сессии.
          </p>
        </header>

        <section class="new-dialog-card">
          <label for="dialog-requirements">
            Новый диалог: опишите требования
          </label>

          <textarea
            id="dialog-requirements"
            v-model="requirements"
            placeholder="Опишите правила системы обычным текстом..."
          ></textarea>

          <div class="new-dialog-actions">
            <button
              type="button"
              class="start-dialog-button" :disabled="!requirements.trim()" @click="activeSessionId === null ? startDialog() : saveChanges()"
            >
              {{ activeSessionId === null ? 'Начать диалог' : 'Сохранить изменения' }}
            </button>
          </div>
        </section>

        <section class="session-history">
          <h3>История сессий</h3>
          <div
            v-if="filteredSessions.length > 0"
            class="history-table"
          >
            <div
              v-for="session in filteredSessions"
              :key="session.id"
              class="history-row"
            >
              <span class="history-name">
                {{ session.title }}
              </span>

              <span class="history-date">
                {{ session.date }}
              </span>

              <span
                class="history-status"
                :class="session.status"
              >
                {{ session.statusText }}
              </span>

              <button
                type="button"
                class="open-session-button" @click="openSession(session)"
              >
                Открыть
              </button>
            </div>
          </div>

          <div v-else class="empty-history">
            <p>{{ sessions.length ? 'Ничего не найдено' : 'У вас пока нет диалогов' }}</p>

            <span>
              {{ sessions.length ? 'Попробуйте изменить поисковый запрос.' : 'Начните первый диалог, чтобы он появился здесь.' }}
            </span>
          </div>
        </section>

      </div>
    </main>
  </div>
</template>

<style scoped>

.dialog-page {
  display: flex;
  width: 100%;
  min-height: 100vh;
  background-color: #f4f6f3;
  color: #19231f;
  font-family: Arial, sans-serif;
  box-sizing: border-box;
}

.dialog-page * {
  box-sizing: border-box;
}
.dialog-sidebar {
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  flex-shrink: 0;
  width: 330px;
  min-height: 100vh;
  padding: 28px 17px 25px;
  background-color: #ffffff;
  border-right: 1px solid #dce2dd;
}

.sidebar-content {
  width: 100%;
}

.sidebar-logo {
  margin: 0 12px 16px;
  font-size: 22px;
  font-weight: 700;
  line-height: 1.3;
}

.new-dialog-button {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 10px;
  width: 100%;
  height: 48px;
  border: none;
  border-radius: 11px;
  background-color: #0f806d;
  color: #ffffff;
  font-family: inherit;
  font-size: 16px;
  cursor: pointer;
}

.new-dialog-button:hover {
  background-color: #0b6c5c;
}

.plus-icon {
  font-size: 25px;
  line-height: 1;
  font-weight: 300;
}

/* ПОИСК */

.sidebar-search {
  width: 100%;
  height: 46px;
  margin-top: 15px;
  padding: 0 13px;
  border: 1px solid #cbd5ce;
  border-radius: 11px;
  outline: none;
  background-color: #ffffff;
  font-family: inherit;
  font-size: 15px;
}

.sidebar-search:focus {
  border-color: #0f806d;
}

.sidebar-search::placeholder {
  color: #858b87;
}

.sidebar-group {
  margin-top: 30px;
}

.sidebar-group-title {
  margin: 0 12px 6px;
  color: #697670;
  font-size: 12px;
  font-weight: 600;
  letter-spacing: 0.3px;
}

.sidebar-session {
  padding: 11px 13px;
  border-radius: 11px;
  cursor: default;
}

.sidebar-session.selected {
  background-color: #e2f3ed;
}

.sidebar-session-title {
  margin: 0;
  color: #19231f;
  font-size: 15px;
  line-height: 1.4;
}

.sidebar-session-description {
  margin: 3px 0 0;
  color: #697670;
  font-size: 13px;
  line-height: 1.4;
}
.sidebar-user {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-top: 28px;
  padding: 16px 12px 0;
  border-top: 1px solid #d5ded8;
}

.user-avatar {
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  width: 42px;
  height: 42px;
  border-radius: 50%;
  background-color: #0f806d;
  color: #ffffff;
  font-size: 15px;
  font-weight: 700;
}

.user-info {
  min-width: 0;
  flex: 1;
}

.user-name {
  margin: 0 0 4px;
  font-size: 14px;
  font-weight: 600;
}

.user-email {
  margin: 0;
  color: #697670;
  font-size: 12px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.logout-link {
  flex-shrink: 0;
  color: #0f806d;
  font-size: 13px;
  text-decoration: underline;
}


.dialog-main {
  flex: 1;
  min-width: 0;
  padding: 40px 50px 60px;
}

.dialog-content {
  width: 100%;
  max-width: 940px;
  margin: 0 auto;
}

.dialog-welcome h2 {
  margin: 0;
  color: #19231f;
  font-size: 34px;
  font-weight: 700;
  line-height: 1.2;
}

.dialog-welcome p {
  margin: 10px 0 0;
  color: #697670;
  font-size: 17px;
}

.new-dialog-card {
  margin-top: 30px;
  padding: 24px 22px 22px;
  background-color: #ffffff;
  border: 1px solid #d5ded8;
  border-radius: 17px;
}

.new-dialog-card label {
  display: block;
  margin-bottom: 10px;
  color: #52615a;
  font-size: 14px;
}

.new-dialog-card textarea {
  display: block;
  width: 100%;
  min-height: 80px;
  padding: 14px 13px;
  resize: vertical;
  border: 1px solid #cbd5ce;
  border-radius: 11px;
  outline: none;
  background-color: #ffffff;
  color: #19231f;
  font-family: inherit;
  font-size: 16px;
  line-height: 1.5;
}

.new-dialog-card textarea::placeholder {
  color: #858b87;
}

.new-dialog-card textarea:focus {
  border-color: #0f806d;
  box-shadow: 0 0 0 3px rgba(15, 128, 109, 0.12);
}

.new-dialog-actions {
  display: flex;
  justify-content: flex-end;
  margin-top: 20px;
}

.start-dialog-button {
  min-height: 48px;
  padding: 0 20px;
  border: none;
  border-radius: 11px;
  background-color: #0f806d;
  color: #ffffff;
  font-family: inherit;
  font-size: 16px;
  cursor: pointer;
}

.start-dialog-button:hover {
  background-color: #0b6c5c;
}

.session-history {
  margin-top: 30px;
}

.session-history h3 {
  margin: 0 0 14px;
  color: #19231f;
  font-size: 21px;
  font-weight: 700;
}

.history-table {
  overflow: hidden;
  border: 1px solid #d5ded8;
  border-radius: 17px;
  background-color: #ffffff;
}

.history-row {
  display: grid;
  grid-template-columns:
    minmax(0, 2.4fr)
    minmax(120px, 1fr)
    minmax(170px, 1.2fr)
    96px;
  align-items: center;
  gap: 16px;
  min-height: 59px;
  padding: 10px 22px;
  border-bottom: 1px solid #e5eae6;
}

.history-row:last-child {
  border-bottom: none;
}

.history-name {
  font-size: 15px;
  font-weight: 500;
}

.history-date {
  color: #697670;
  font-size: 14px;
}

.history-status {
  justify-self: start;
  display: inline-block;
  padding: 6px 11px;
  border-radius: 20px;
  font-size: 13px;
  white-space: nowrap;
}

.history-status.error {
  color: #a93620;
  background-color: #fce9e5;
}

.history-status.success {
  color: #076b5b;
  background-color: #e2f3ed;
}

.history-status.warning {
  color: #8b6100;
  background-color: #fff1d1;
}

.open-session-button {
  min-height: 36px;
  padding: 0 14px;
  border: 1px solid #cbd5ce;
  border-radius: 11px;
  background-color: #ffffff;
  color: #19231f;
  font-family: inherit;
  font-size: 14px;
  cursor: pointer;
}

.open-session-button:hover {
  background-color: #f4f6f3;
}


.empty-history {
  padding: 35px 20px;
  border: 1px solid #d5ded8;
  border-radius: 17px;
  background-color: #ffffff;
  text-align: center;
}

.empty-history p {
  margin: 0 0 8px;
  color: #19231f;
  font-size: 16px;
  font-weight: 600;
}

.empty-history span {
  color: #697670;
  font-size: 14px;
}


@media (max-width: 1100px) {
  .dialog-sidebar {
    width: 280px;
  }

  .dialog-main {
    padding: 35px 28px;
  }

  .history-row {
    grid-template-columns:
      minmax(0, 2fr)
      110px
      150px
      85px;
    gap: 10px;
    padding: 10px 15px;
  }
}

@media (max-width: 800px) {
  .dialog-page {
    flex-direction: column;
  }

  .dialog-sidebar {
    width: 100%;
    min-height: auto;
  }

  .sidebar-user {
    margin-top: 25px;
  }

  .dialog-main {
    padding: 28px 18px;
  }

  .dialog-welcome h2 {
    font-size: 28px;
  }

  .history-row {
    grid-template-columns: 1fr auto;
    gap: 10px;
  }

  .history-date {
    grid-column: 1;
  }

  .history-status {
    grid-column: 1;
  }

  .open-session-button {
    grid-column: 2;
    grid-row: 1 / 4;
  }
}

/* Сворачивание панели истории */
.dialog-sidebar { position: relative; overflow: hidden; transition: width .25s ease, padding .25s ease, border-color .25s ease; }
.dialog-sidebar.collapsed { width: 0 !important; padding-left: 0; padding-right: 0; border-right-color: transparent; }
.sidebar-content, .sidebar-user { min-width: 246px; }
.sidebar-close { position: absolute; right: 12px; top: 18px; border: 0; background: #e2f3ed; color: #0f806d; border-radius: 8px; width: 30px; height: 30px; font-size: 26px; cursor: pointer; }
.sidebar-toggle { position: absolute; top: 20px; left: 12px; z-index: 2; width: 35px; height: 35px; border: 1px solid #cbd5ce; background: white; color: #0f806d; border-radius: 9px; cursor: pointer; font-size: 21px; }
.dialog-main { position: relative; }
.dialog-page:not(.sidebar-collapsed) .sidebar-toggle { display: none; }
.dialog-page.sidebar-collapsed .dialog-main { padding-top: 65px; }
.sidebar-session { cursor: pointer; }
.start-dialog-button:disabled { opacity: .5; cursor: not-allowed; }
@media (max-width: 800px) {
  .dialog-sidebar.collapsed { height: 0; min-height: 0; padding-top: 0; padding-bottom: 0; }
  .dialog-sidebar { transition: height .25s ease, padding .25s ease; }
}
</style>
