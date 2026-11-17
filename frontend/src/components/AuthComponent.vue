<template>
  <aside class="auth-panel">
    <div class="auth-header">
      <h2 class="auth-title">Агент-аналитик</h2>
      <button type="button" class="auth-close" @click="$router.push('/')">✕</button>
    </div>
    <div class="auth-tabs">
      <RouterLink
        type="button"
        to="/register"
        class="auth-tab"
        :class="{ active: mode === 'register' }"
        @click="mode = 'register'"
      >
        Регистрация
      </RouterLink>

      <RouterLink
        type="button"
        to="/login"
        class="auth-tab"
        :class="{ active: mode === 'login' }"
        @click="mode = 'login'"
      >
        Вход
      </RouterLink>
    </div>

    <slot />

    <p class="auth-switch">
      {{ mode === 'register' ? 'Есть аккаунт?' : 'Нет аккаунта?' }}

      <RouterLink :to="mode === 'register' ? '/login' : '/register'" class="auth-link">
        {{ mode === 'register' ? 'Войти' : 'Зарегистрироваться' }}
      </RouterLink>
    </p>

    <hr class="auth-divider" />

    <nav class="auth-menu">
      <ul>
        <li><a href="#">Как это работает</a></li>
        <li><a href="#">Примеры требований</a></li>
      </ul>
    </nav>
  </aside>
</template>

<script setup lang="ts">
defineProps<{
  mode: 'register' | 'login'
}>()
</script>
