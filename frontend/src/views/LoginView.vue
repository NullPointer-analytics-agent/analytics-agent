<script setup lang="ts">
import { reactive, ref } from 'vue'
import api from '@/api'
import { useAuthStore } from '@/stores/auth'
import { useRouter } from 'vue-router'
import AuthCard from '@/components/AuthComponent.vue'
import '@/assets/auth.css'

const authStore = useAuthStore()
const router = useRouter()

interface loginForm {
  email: string
  password: string
}

const form = reactive<loginForm>({
  email: '',
  password: '',
})

const isLoading = ref(false)

interface error {
  message: string
  status: number
}

const validate_errors = reactive<loginForm>({
  email: '',
  password: '',
})

const error = reactive<error>({
  message: '',
  status: 0,
})

function validate_form() {
  let is_ok = true
  validate_errors.email = ''
  validate_errors.password = ''
  const regex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

  if (form.email.trim() === '') {
    validate_errors.email = 'Email обязателен'
    is_ok = false
  } else if (!regex.test(form.email)) {
    validate_errors.email = 'Некорректный email'
    is_ok = false
  }
  if (form.password.trim() === '') {
    validate_errors.password = 'Пароль обязателен'
    is_ok = false
  } else if (form.password.length < 8 || form.password.length > 72) {
    validate_errors.password = 'Пароль должен содержать от 8 до 72 символов'
    is_ok = false
  }
  return is_ok
}

async function handleLogin() {
  isLoading.value = false
  error.message = ''
  error.status = 0
  if (validate_form()) {
    try {
      isLoading.value = true
      const response = await api.post('/auth/login', form)
      console.log(response.data)
      authStore.setAuth(response.data.token, {
        id: response.data.id,
        name: response.data.name,
        email: response.data.email,
      })
      router.push('/dialog')
    } catch (e: any) {
      if (!e.response) {
        error.message = 'Сервер не отвечает'
      } else {
        error.status = e.response.status
        if (error.status == 401) {
          error.message = 'Неверный email или пароль'
        } else {
          error.message = 'Произошла ошибка'
        }
      }
      console.log(e)
    } finally {
      isLoading.value = false
    }
  }
}
</script>

<template>
  <AuthCard mode="login">
    <form class="auth-form" @submit.prevent="handleLogin">
      <div class="auth-field">
        <label for="email">Email</label>
        <input v-model="form.email" id="email" type="text" placeholder="Ваш email" />
        <p v-if="validate_errors.email" class="error validation">{{ validate_errors.email }}</p>
      </div>

      <div class="auth-field">
        <label for="password">Пароль</label>
        <input v-model="form.password" id="password" type="password" placeholder="Ваш пароль" />
        <p v-if="validate_errors.password" class="error validation">
          {{ validate_errors.password }}
        </p>
      </div>

      <button type="submit" class="auth-button" :disabled="isLoading">
        {{ isLoading ? 'Загрузка...' : 'Войти' }}
      </button>
      <p v-if="error.message" class="error">{{ error.message }}</p>
    </form>
  </AuthCard>
</template>

<style></style>
