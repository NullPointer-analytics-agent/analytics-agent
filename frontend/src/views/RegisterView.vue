<script setup lang="ts">
import { reactive, ref } from 'vue'
//import { useRouter } from 'vue-router'
import api from '@/api'
import '@/assets/auth.css'

interface registerForm {
  name: string
  email: string
  password: string
}

const form = reactive<registerForm>({
  name: '',
  email: '',
  password: '',
})

const error = ref('')
const isLoading = ref(false)
//const router = useRouter()

async function handleRegister() {
  isLoading.value = true
  error.value = ''

  try {
    const response = await api.post('/auth/register', form)
    console.log(response.data)
  } catch {
    console.log('error')
  }
}
</script>

<template>
  <aside class="auth-panel">
    <div class="auth-header">
      <h2 class="auth-title">Агент-аналитик</h2>
      <button type="button" class="auth-close">✕</button>
    </div>

    <div class="auth-tabs">
      <button type="button" class="auth-tab active">Регистрация</button>
      <RouterLink to="/login" type="button" class="auth-tab">Вход</RouterLink>
    </div>

    <form class="auth-form" @submit.prevent="handleRegister">
      <div class="auth-field">
        <label for="name">Имя</label>
        <input v-model="form.name" id="name" type="text" placeholder="Ваше имя" />
        {{ form.name }}
      </div>

      <div class="auth-field">
        <label for="email">Email</label>
        <input v-model="form.email" id="email" type="email" placeholder="Ваш email" />
      </div>

      <div class="auth-field">
        <label for="password">Пароль</label>
        <input v-model="form.password" id="password" type="password" placeholder="Ваш пароль" />
      </div>

      <button type="submit" class="auth-button">Зарегистрироваться</button>
    </form>

    <p class="auth-switch">
      Есть аккаунт? <RouterLink class="link-underline" to="/login">Войти</RouterLink>
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

<style>
@import '@/assets/auth.css';
</style>
