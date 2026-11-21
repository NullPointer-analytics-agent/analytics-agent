<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { isBehaviourModelDto, type BehaviourModelDto, type ResponseFrame } from '@/api/behaviour'
import { useRouter } from 'vue-router'

const authStore = useAuthStore()
const router = useRouter()

interface Session {
  id: number
  title: string
  description: string
  date: string
  createdAt: string
  requirements: string
  status: 'error' | 'success' | 'warning'
  isLoading: boolean
  statusText: string
  model: BehaviourModelDto | null
}

const storageKey = computed(() => `analytics-agent-sessions-${authStore.user?.id ?? 'guest'}`)
function loadSessions(): Session[] {
  try {
    const saved = localStorage.getItem(storageKey.value)
    const parsed: unknown = saved ? JSON.parse(saved) : []
    return Array.isArray(parsed)
      ? parsed.filter(
          (item): item is Session =>
            item !== null &&
            typeof item === 'object' &&
            typeof item.id === 'number' &&
            typeof item.requirements === 'string' &&
            typeof item.createdAt === 'string' &&
            typeof item.title === 'string',
        )
      : []
  } catch {
    return []
  }
}
const sessions = ref<Session[]>(loadSessions())
const sidebarOpen = ref(true)
const searchQuery = ref('')
const requirements = ref('')
const activeSessionId = ref<number | null>(null)
const loadingSessionId = ref<number | null>(null)

const abortControllers = new Map<number | null, AbortController>()

const isStartDialog = ref(false)
const isLoading = ref(false)
const isEditing = ref(true)
const errorLLM = ref<string | null>(null)
let abortController: AbortController | null = null

const modelResult = ref<BehaviourModelDto | null>(null)

const userName = computed(() => authStore.user?.name || 'Имя Фамилия')

const userEmail = computed(() => authStore.user?.email || 'name@mail.ru')

const firstName = computed(() => userName.value.split(' ')[0] || 'Имя')

const initials = computed(() =>
  userName.value
    .trim()
    .split(/\s+/)
    .slice(0, 2)
    .map((word) => word.charAt(0))
    .join('')
    .toUpperCase(),
)

const filteredSessions = computed(() =>
  sessions.value.filter((session) =>
    `${session.title} ${session.description} ${session.requirements}`
      .toLowerCase()
      .includes(searchQuery.value.toLowerCase().trim()),
  ),
)
function groupFor(session: Session): 'today' | 'yesterday' | 'earlier' {
  const date = new Date(session.createdAt)
  const now = new Date()
  if (date.toDateString() === now.toDateString()) return 'today'
  const yesterday = new Date(now.getFullYear(), now.getMonth(), now.getDate() - 1)
  if (date.toDateString() === yesterday.toDateString()) return 'yesterday'
  return 'earlier'
}
const todaySessions = computed(() => filteredSessions.value.filter((s) => groupFor(s) === 'today'))
const yesterdaySessions = computed(() =>
  filteredSessions.value.filter((s) => groupFor(s) === 'yesterday'),
)
const earlierSessions = computed(() =>
  filteredSessions.value.filter((s) => groupFor(s) === 'earlier'),
)

async function askLLM(sessionId: number | null) {
  if (!requirements.value.trim()) return

  // Если для этой сессии уже есть запрос — отменяем его
  if (abortControllers.has(sessionId)) {
    abortControllers.get(sessionId)?.abort()
  }

  const controller = new AbortController()
  abortControllers.set(sessionId, controller)

  const session = sessions.value.find((s) => s.id === sessionId)
  if (!session) return
  session.isLoading = true
  loadingSessionId.value = sessionId
  isStartDialog.value = true
  isEditing.value = false
  errorLLM.value = null
  modelResult.value = null
  session.model = null

  try {
    const response = await fetch(`${import.meta.env.VITE_LLM_API_URL}/chat`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ Prompt: requirements.value }),
      signal: controller.signal,
    })

    if (!response.ok) throw new Error(`Ошибка сервера: ${response.status}`)
    if (!response.body) throw new Error('Нет тела ответа')

    const reader = response.body.getReader()
    const decoder = new TextDecoder('utf-8')
    let buffer = ''

    while (true) {
      const { done, value } = await reader.read()
      if (done) break

      buffer += decoder.decode(value, { stream: true })
      const lines = buffer.split('\n')
      buffer = lines.pop() || ''

      for (const line of lines) {
        if (!line.trim()) continue
        try {
          const frame: ResponseFrame = JSON.parse(line)
          if (frame.type === 'chunk_dto' || frame.type === 'system_status_dto') continue

          if (isBehaviourModelDto(frame)) {
            // Ищем сессию, к которой относится этот запрос
            const session = sessions.value.find((s) => s.id === sessionId)

            if (!frame.IsValidPrompt) {
              if (session) {
                session.status = 'error'
                session.statusText = frame.ReasonIfInvalid || 'Ошибка'
                session.model = null
              }

              if (activeSessionId.value === sessionId) {
                modelResult.value = null
                errorLLM.value = frame.ReasonIfInvalid || 'Ошибка'
                isEditing.value = true
              }
            } else {
              if (session) {
                session.model = frame
                session.status = 'success'
                session.statusText = 'Успешно'
              }
              // Показываем модель только если мы смотрим на эту сессию
              if (activeSessionId.value === sessionId) {
                modelResult.value = frame
              }
            }
          }
        } catch (e) {
          console.error('Ошибка парсинга:', e)
        }
      }
    }
  } catch (error: any) {
    if (error.name !== 'AbortError') {
      console.error('Ошибка:', error)
      const session = sessions.value.find((s) => s.id === sessionId)
      if (session) {
        session.status = 'error'
        session.statusText = 'Ошибка при запросе к LLM'
        session.model = null
      }

      if (activeSessionId.value === sessionId) {
        modelResult.value = null
        errorLLM.value = 'Ошибка при запросе к LLM'
        isEditing.value = true
      }
    }
  } finally {
    // Проверяем, что это всё ещё актуальный запрос для сессии
    if (abortControllers.get(sessionId) === controller) {
      abortControllers.delete(sessionId)
      session.isLoading = false
    }

    if (activeSessionId.value === sessionId) {
      isLoading.value = false
    }

    if (loadingSessionId.value === sessionId) {
      loadingSessionId.value = null
    }
  }
}

function formatPropertyName(type: string) {
  const map: Record<string, string> = {
    Invariant: 'Инвариант',
    ForbiddenState: 'Запрещённое состояние',
    Reachable: 'Достижимость',
    DeadlockFree: 'Отсутствие тупиков',
  }
  return map[type] || type
}

function edit() {
  const currentSessionId = activeSessionId.value
  if (currentSessionId !== null && abortControllers.has(currentSessionId)) {
    // Обрываем запрос именно для этой сессии
    abortControllers.get(currentSessionId)?.abort()
    abortControllers.delete(currentSessionId)
  }

  const session = sessions.value.find((s) => s.id === currentSessionId)
  if (session) {
    session.status = 'warning'
    session.statusText = ''
  }

  isLoading.value = false
  isEditing.value = true
  errorLLM.value = null
}

function handleLogout() {
  authStore.logout()
  router.push('/login')
}

watch(
  sessions,
  (value) => {
    try {
      localStorage.setItem(storageKey.value, JSON.stringify(value))
    } catch {
      /* Storage unavailable */
    }
  },
  { deep: true },
)
watch(storageKey, () => {
  sessions.value = loadSessions()
  activeSessionId.value = null
  requirements.value = ''
})

function newDialog() {
  activeSessionId.value = null
  requirements.value = ''
  modelResult.value = null
  errorLLM.value = null
  isEditing.value = true
  isStartDialog.value = false
}

function startDialog(): number | null {
  const value = requirements.value.trim()
  if (!value) return null
  const now = new Date()
  const session: Session = {
    id: now.getTime(),
    title: value.length > 45 ? `${value.slice(0, 45)}…` : value,
    description: value.length > 85 ? `${value.slice(0, 85)}…` : value,
    requirements: value,
    date: now.toLocaleString('ru-RU', {/* ... */}),
    createdAt: now.toISOString(),
    status: 'warning',
    statusText: 'Черновик',
    isLoading: true,
    model: null,
  }
  sessions.value.unshift(session)
  activeSessionId.value = session.id
  return session.id
}

function handleStartDialog() {
  let sessionId = activeSessionId.value

  if (sessionId === null) {
    sessionId = startDialog()
  } else {
    saveChanges()
  }

  if (sessionId !== null) {
    askLLM(sessionId)
  }
}

function openSession(session: Session) {
  activeSessionId.value = session.id
  requirements.value = session.requirements
  modelResult.value = session.model
  if (session.status !== 'success') errorLLM.value = session.statusText
  else errorLLM.value = null
  isStartDialog.value = true
  isEditing.value = false
  modelResult.value = session.model
}

function saveChanges() {
  const session = sessions.value.find((s) => s.id === activeSessionId.value)
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
      <!-- Закреплённая верхняя часть -->
      <div class="sidebar-header">
        <button
          type="button"
          class="sidebar-close"
          aria-label="Свернуть историю"
          @click="sidebarOpen = false"
        >
          ‹
        </button>

        <h1 class="sidebar-logo">Агент-аналитик</h1>

        <button type="button" class="new-dialog-button" @click="newDialog">
          <span class="plus-icon">+</span>
          Новый диалог
        </button>

        <input
          class="sidebar-search"
          v-model="searchQuery"
          type="text"
          placeholder="Поиск по сессиям"
        />
      </div>

      <!-- Прокручиваемая история -->
      <div class="sidebar-history">
        <!-- СЕГОДНЯ -->
        <div v-if="todaySessions.length > 0" class="sidebar-group">
          <p class="sidebar-group-title">СЕГОДНЯ</p>

          <div
            v-for="session in todaySessions"
            :key="session.id"
            class="sidebar-session"
            :class="{ selected: activeSessionId === session.id }"
            @click="openSession(session)"
          >
            <p class="sidebar-session-title">
              {{ session.title }}
            </p>

            <p class="sidebar-session-description">
              {{ session.description }}
            </p>
          </div>
        </div>

        <!-- ВЧЕРА -->
        <div v-if="yesterdaySessions.length > 0" class="sidebar-group">
          <p class="sidebar-group-title">ВЧЕРА</p>

          <div
            v-for="session in yesterdaySessions"
            :key="session.id"
            class="sidebar-session"
            :class="{ selected: activeSessionId === session.id }"
            @click="openSession(session)"
          >
            <p class="sidebar-session-title">
              {{ session.title }}
            </p>

            <p class="sidebar-session-description">
              {{ session.description }}
            </p>
          </div>
        </div>

        <!-- РАНЕЕ -->
        <div v-if="earlierSessions.length > 0" class="sidebar-group">
          <p class="sidebar-group-title">РАНЕЕ</p>

          <div
            v-for="session in earlierSessions"
            :key="session.id"
            class="sidebar-session"
            :class="{ selected: activeSessionId === session.id }"
            @click="openSession(session)"
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

      <!-- Закреплённая нижняя часть -->
      <div class="sidebar-user">
        <div class="user-avatar">
          {{ initials }}
        </div>

        <div class="user-info">
          <p class="user-name">{{ userName }}</p>
          <p class="user-email">{{ userEmail }}</p>
        </div>

        <button @click="handleLogout()" class="logout-link">Выйти</button>
      </div>
    </aside>

    <main class="dialog-main">
      <button
        type="button"
        class="sidebar-toggle"
        :aria-label="sidebarOpen ? 'Свернуть историю' : 'Открыть историю'"
        @click="sidebarOpen = !sidebarOpen"
      >
        {{ sidebarOpen ? '‹' : '☰' }}
      </button>
      <div class="dialog-content">
        <!-- Приветствие -->
        <header v-if="!isStartDialog" class="dialog-welcome">
          <h2>Здравствуйте, {{ firstName }}</h2>

          <p>Начните новый диалог или вернитесь к прошлой сессии.</p>
        </header>

        <section class="new-dialog-card" :class="{ 'card-transparent': !isEditing }">
          <label v-if="isEditing" for="dialog-requirements">
            Новый диалог: опишите требования
          </label>

          <textarea
            v-if="isEditing"
            id="dialog-requirements"
            v-model="requirements"
            placeholder="Опишите правила системы обычным текстом..."
          ></textarea>

          <div v-else class="prompt-readonly-text">
            {{ requirements }}
            <div>
              <p @click="edit()" class="edit">
                Редактировать
                <span class="edit-icon"></span>
              </p>
            </div>
          </div>

          <div class="new-dialog-actions">
            <button
              type="button"
              @click="handleStartDialog"
              class="start-dialog-button"
              :disabled="!requirements.trim()"
              v-if="isEditing"
            >
              {{ activeSessionId === null ? 'Начать диалог' : 'Сохранить изменения' }}
            </button>
          </div>
        </section>
        <div
          v-if="sessions.find((s) => s.id === activeSessionId)?.isLoading"
          class="thinking-spinner"
        >
          <div class="spinner"></div>
          <p class="thinking-text">Я очень стараюсь. Подождите минутку))</p>
        </div>
        <p
          v-if="
            activeSessionId && sessions.find((s) => s.id === activeSessionId)?.status === 'error'
          "
          class="error-content"
        >
          {{ errorLLM }}
        </p>

        <div v-if="modelResult" class="result-content">
          <div class="result-sections-wrapper">
            <!-- 1. Переменные состояния (Definitions) -->
            <section class="result-section">
              <h3 class="result-section-title">Переменные состояния</h3>

              <div class="behaviour-table">
                <div class="behaviour-table-header">
                  <div>Переменная</div>
                  <div>Тип</div>
                  <div>Допустимые значения</div>
                </div>

                <div
                  v-for="def in modelResult.Definitions"
                  :key="def.VariableName"
                  class="behaviour-table-row"
                >
                  <div class="behaviour-var-name">{{ def.VariableName }}</div>
                  <div class="behaviour-var-type">
                    <span class="type-badge" :class="'type-' + def.VariableType.toLowerCase()">
                      {{ def.VariableType }}
                    </span>
                  </div>
                  <div class="behaviour-var-values">
                    <template v-if="def.VariableType === 'Enum'">
                      <span v-for="val in def.EnumValues" :key="val" class="value-badge">{{
                        val
                      }}</span>
                    </template>
                    <template v-else-if="def.VariableType === 'Integer'">
                      <span class="value-badge">{{ def.IntegerMin }} — {{ def.IntegerMax }}</span>
                    </template>
                    <template v-else>
                      <span class="value-badge">true, false</span>
                    </template>
                  </div>
                </div>
              </div>
            </section>

            <!-- 2. Начальное состояние (InitialStates) -->
            <section class="result-section">
              <h3 class="result-section-title">Начальное состояние</h3>

              <div class="initial-state-card">
                <div
                  v-for="init in modelResult.InitialStates"
                  :key="init.ReferencedVariableName"
                  class="initial-state-item"
                >
                  <span class="init-var-name">{{ init.ReferencedVariableName }}</span>
                  <span class="init-eq">=</span>
                  <span class="init-val">{{ init.Value }}</span>
                </div>
              </div>
            </section>

            <!-- 3. Свойства (Properties) -->
            <section v-if="modelResult" class="result-section">
              <h3 class="result-section-title">Проверяемые свойства</h3>

              <div class="properties-list">
                <div
                  v-for="(prop, index) in modelResult?.Properties"
                  :key="index"
                  class="property-card"
                >
                  <!-- Бейдж типа свойства -->
                  <div class="property-header">
                    <span class="property-badge" :class="'prop-' + prop.PropertyType.toLowerCase()">
                      {{ formatPropertyName(prop.PropertyType) }}
                    </span>
                  </div>

                  <!-- Сама формула (Guard) -->
                  <div class="property-guard">
                    <code>{{ prop.Guard || 'Нет дополнительных условий' }}</code>
                  </div>
                </div>
              </div>
            </section>

            <!-- 3. Переходы (Transitions) -->
            <section class="result-section">
              <h3 class="result-section-title">Переходы (События)</h3>

              <div class="transitions-grid">
                <div
                  v-for="transition in modelResult.Transitions"
                  :key="transition.Name"
                  class="transition-card"
                >
                  <div class="transition-header">
                    <span class="transition-name">{{ transition.Name }}</span>
                  </div>

                  <div class="transition-body">
                    <div class="transition-row">
                      <span class="transition-label">Условие (Guard):</span>
                      <code class="transition-code">{{ transition.Guard || 'Всегда' }}</code>
                    </div>

                    <div class="transition-row">
                      <span class="transition-label">Эффекты:</span>
                      <div class="transition-effects">
                        <span
                          v-for="effect in transition.Effects"
                          :key="effect.ReferencedVariableName"
                          class="effect-badge"
                        >
                          {{ effect.ReferencedVariableName }} := {{ effect.Value }}
                        </span>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </section>
          </div>
        </div>

        <section v-if="!isStartDialog" class="session-history">
          <h3>История сессий</h3>
          <div v-if="filteredSessions.length > 0" class="history-table">
            <div v-for="session in filteredSessions" :key="session.id" class="history-row">
              <span class="history-name">
                {{ session.title }}
              </span>

              <span class="history-date">
                {{ session.date }}
              </span>

              <span class="history-status" :class="session.status">
                {{ { error: 'Ошибка', success: 'Успешно', warning: 'Черновик' }[session.status] }}
              </span>
              <button type="button" class="open-session-button" @click="openSession(session)">
                Открыть
              </button>
            </div>
          </div>

          <div v-else class="empty-history">
            <p>{{ sessions.length ? 'Ничего не найдено' : 'У вас пока нет диалогов' }}</p>

            <span>
              {{
                sessions.length
                  ? 'Попробуйте изменить поисковый запрос.'
                  : 'Начните первый диалог, чтобы он появился здесь.'
              }}
            </span>
          </div>
        </section>
      </div>
    </main>
  </div>
</template>

<style scoped>
.error-content {
  margin: 0;
  color: #b91c1c;
  font-size: 14px;
  line-height: 1.4;
}

.thinking-spinner {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 16px;
  padding: 40px;
}

.spinner {
  width: 48px;
  height: 48px;
  border: 3px solid rgba(34, 224, 255, 0.2);
  border-top-color: #0f806d;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.thinking-text {
  font-size: 16px;
  font-weight: 500;
  color: #19231f;
  margin: 0;
}

.edit {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  color: #97a69f;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  transition: color 0.2s;
}

.edit:hover {
  color: #0f806d;
}

.edit-icon {
  display: inline-block;
  width: 16px;
  height: 16px;
  background-color: currentColor;
  -webkit-mask: url('@/assets/image.svg') no-repeat center / contain;
  mask: url('@/assets/image.svg') no-repeat center / contain;
}

.result-sections-wrapper {
  display: flex;
  flex-direction: column;
  gap: 24px;
  margin-top: 20px;
}

.result-section-title {
  margin: 0 0 12px;
  color: #19231f;
  font-size: 18px;
  font-weight: 700;
}

.behaviour-table {
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  background-color: #ffffff;
  overflow: hidden;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.02);
}

.behaviour-table-header {
  display: grid;
  grid-template-columns: minmax(150px, 1fr) 110px 2fr;
  gap: 16px;
  padding: 14px 20px;
  background-color: #f8fafc;
  color: #64748b;
  font-size: 13px;
  font-weight: 600;
  border-bottom: 1px solid #e2e8f0;
}

.prompt-readonly-text {
  display: block;
  width: 100%;
  min-height: 80px;
  padding: 12px 12px;
  border: 1px solid transparent;
  border-radius: 11px;
  color: #19231f;
  font-family: inherit;
  font-size: 16px;
  line-height: 1.5;
  white-space: pre-wrap;
  word-break: break-word;
  color: #19231f;
  background-color: #ffffff;
  border-left: 4px solid #0f806d;
}

.new-dialog-card.card-transparent {
  border: none;
  background-color: transparent;
  padding: 0;
}

.behaviour-table-row {
  display: grid;
  grid-template-columns: minmax(150px, 1fr) 110px 2fr;
  align-items: center;
  gap: 16px;
  padding: 16px 20px;
  border-bottom: 1px solid #f1f5f9;
  transition: background-color 0.2s;
}

.behaviour-table-row:last-child {
  border-bottom: none;
}

.behaviour-table-row:hover {
  background-color: #f8fafc;
}

.behaviour-var-name {
  font-size: 15px;
  font-weight: 500;
  color: #0f172a;
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
}

.type-badge {
  display: inline-block;
  padding: 6px 10px;
  border-radius: 16px;
  font-size: 12px;
  font-weight: 600;
  letter-spacing: 0.2px;
  text-align: center;
}

.type-enum {
  background-color: #e3f2ec;
  color: #0f806d;
}

.type-integer {
  background-color: #e8f0fe;
  color: #2b57b6;
}

.type-boolean {
  background-color: #fef3c7;
  color: #c47e16;
}

.behaviour-var-values {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.value-badge {
  display: inline-block;
  padding: 3px 8px;
  border-radius: 6px;
  background-color: #f1f5f9;
  color: #475569;
  font-size: 13px;
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
}

.initial-state-card {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  padding: 20px;
  background-color: #e2f3ed;
  border: 1px solid #c8e6d9;
  border-radius: 17px;
}

.initial-state-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 14px;
  background-color: #ffffff;
  border-radius: 11px;
  box-shadow: 0 2px 4px rgba(15, 128, 109, 0.05);
  font-family: monospace;
  font-size: 14px;
}

.init-var-name {
  color: #19231f;
  font-weight: 600;
}

.init-eq {
  color: #0f806d;
  font-weight: bold;
}

.init-val {
  color: #076b5b;
  background-color: #e2f3ed;
  padding: 2px 6px;
  border-radius: 4px;
}

.properties-list {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 16px;
}

.property-card {
  background-color: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  padding: 16px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.02);
  display: flex;
  flex-direction: column;
  gap: 12px;
  transition: border-color 0.2s;
}

.property-card:hover {
  border-color: #cbd5e1;
}

.property-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.property-badge {
  display: inline-block;
  padding: 4px 10px;
  border-radius: 6px;
  font-size: 13px;
  font-weight: 600;
}

.prop-invariant {
  background-color: #eff6ff;
  color: #1d4ed8;
}

.prop-forbiddenstate {
  background-color: #fef2f2;
  color: #b91c1c;
}

.prop-reachable {
  background-color: #ecfdf5;
  color: #047857;
}

.prop-deadlockfree {
  background-color: #f5f3ff;
  color: #6d28d9;
}

.property-guard {
  background-color: #f8fafc;
  padding: 10px 14px;
  border-radius: 8px;
  border: 1px solid #e2e8f0;
  overflow-x: auto;
}

.property-guard code {
  color: #334155;
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
  font-size: 13px;
  line-height: 1.5;
  white-space: pre-wrap;
  word-break: break-word;
}

.transitions-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: 16px;
}

.transition-card {
  background-color: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  overflow: hidden;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.02);
  display: flex;
  flex-direction: column;
}

.transition-header {
  background-color: #f8fafc;
  padding: 10px 16px;
  border-bottom: 1px solid #e2e8f0;
}

.transition-name {
  font-size: 15px;
  font-weight: 700;
  color: #0f172a;
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
}

.transition-body {
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.transition-row {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.transition-label {
  font-size: 12px;
  font-weight: 600;
  color: #64748b;
  text-transform: uppercase;
  letter-spacing: 0.3px;
}

.transition-code {
  background-color: #f1f5f9;
  padding: 6px 10px;
  border-radius: 6px;
  font-size: 13px;
  color: #334155;
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
  word-break: break-word;
}

.transition-effects {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.effect-badge {
  display: inline-block;
  padding: 4px 8px;
  border-radius: 6px;
  background-color: #e2f3ed;
  color: #076b5b;
  font-size: 12px;
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
  font-weight: 600;
}

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
  flex-shrink: 0;

  width: 330px;
  height: 100vh;
  min-height: 0;

  padding: 28px 17px 25px;

  background-color: #ffffff;
  border-right: 1px solid #dce2dd;

  position: sticky;
  top: 0;

  overflow: hidden;
}

/* Верхняя закреплённая часть */
.sidebar-header {
  flex-shrink: 0;
  width: 100%;
}

/* Область прокрутки */
.sidebar-history {
  flex: 1;
  min-height: 0;
  width: 100%;
  overflow-y: auto;
  overflow-x: hidden;
}

/* Нижняя закреплённая часть */
.sidebar-user {
  flex-shrink: 0;
  display: flex;
  align-items: center;
  gap: 12px;

  margin-top: 16px;
  padding: 16px 12px 0;

  border-top: 1px solid #d5ded8;
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
  padding: 0;
  border: none;
  background: none;
  color: #0f806d;
  font-family: inherit;
  font-size: 13px;
  text-decoration: underline;
  cursor: pointer;
}

.dialog-main {
  flex: 1;
  min-width: 0;
  padding: 40px 50px 60px;
}

.dialog-content {
  width: 100%;
  max-width: 1100px;
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
  height: 150px;
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

.dialog-sidebar {
  position: fixed;
  top: 0;
  bottom: 0;
  left: 0;
  z-index: 10;
  height: 100vh;
  transition:
    width 0.25s ease,
    padding 0.25s ease,
    transform 0.25s ease,
    border-color 0.25s ease;
}

.dialog-sidebar.collapsed {
  width: 0 !important;
  padding-left: 0;
  padding-right: 0;
  border-right-color: transparent;
  transform: translateX(-100%);
}

.dialog-main {
  margin-left: 330px;
  transition: margin-left 0.25s ease;
  position: relative;
}

.dialog-page.sidebar-collapsed .dialog-main {
  margin-left: 0;
  padding-top: 65px;
}

.sidebar-content,
.sidebar-user {
  min-width: 246px;
}

.sidebar-close {
  position: absolute;
  right: 12px;
  top: 18px;
  border: 0;
  background: #e2f3ed;
  color: #0f806d;
  border-radius: 8px;
  width: 30px;
  height: 30px;
  font-size: 26px;
  cursor: pointer;
}

.sidebar-toggle {
  position: absolute;
  top: 20px;
  left: 12px;
  z-index: 20;
  width: 35px;
  height: 35px;
  border: 1px solid #cbd5ce;
  background: white;
  color: #0f806d;
  border-radius: 9px;
  cursor: pointer;
  font-size: 21px;
}

.dialog-page:not(.sidebar-collapsed) .sidebar-toggle {
  display: none;
}

.sidebar-session {
  cursor: pointer;
}
.start-dialog-button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

@media (max-width: 800px) {
  .dialog-sidebar {
    position: fixed;
    width: 100%;
    height: auto;
    bottom: auto;
  }

  .dialog-main {
    margin-left: 0;
  }

  .dialog-sidebar.collapsed {
    width: 100% !important;
    height: 0;
    padding-top: 0;
    padding-bottom: 0;
    transform: none;
  }
}
</style>
