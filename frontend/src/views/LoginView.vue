<script setup lang="ts">
import '@/assets/auth.css'
</script>

<template>
  <aside class="auth-panel">
    <div class="auth-header">
      <h2 class="auth-title">Агент-аналитик</h2>
      <button type="button" class="auth-close">✕</button>
    </div>

    <div class="auth-tabs">
      <RouterLink type="button" class="auth-tab" to="/register">Регистрация</RouterLink>
      <button type="button" class="auth-tab active">Вход</button>
    </div>

    <form class="auth-form">
      <div class="auth-field">
        <label for="email">Email</label>
        <input id="email" type="email" placeholder="Ваш email" />
      </div>

      <div class="auth-field">
        <label for="password">Пароль</label>
        <input id="password" type="password" placeholder="Ваш пароль" />
      </div>

      <button type="submit" class="auth-button">Войти</button>
    </form>

    <p class="auth-switch">
      Есть аккаунт?
      <RouterLink class="link-underline" to="/register">Зарегистрироваться</RouterLink>
    </p>

    <hr class="auth-divider" />

    <nav class="auth-menu">
      <ul>
        <li><a href="#">Главная</a></li>
        <li><a href="#">Как это работает</a></li>
        <li><a href="#">Примеры требований</a></li>
      </ul>
    </nav>
  </aside>
</template>
