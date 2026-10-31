<script setup lang="ts">
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import api from '@/api'
import AuthCard from '@/components/AuthComponent.vue'
import '@/assets/auth.css'

const router = useRouter()

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

const isLoading = ref(false)

interface error {
  message: string
  status: number
}

const validate_errors = reactive<registerForm>({
  name: '',
  email: '',
  password: '',
})

const error = reactive<error>({
  message: '',
  status: 0,
})

function validate_form() {
  let is_ok = true
  validate_errors.name = ''
  validate_errors.email = ''
  validate_errors.password = ''
  const regex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

  if (form.name.trim() === '') {
    validate_errors.name = 'Имя обязательно'
    is_ok = false
  } else if (form.name.length > 64) {
    validate_errors.name = 'Имя должно быть не длиннее 64 символов'
    is_ok = false
  }
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

async function handleRegister() {
  isLoading.value = false
  error.message = ''
  error.status = 0
  if (validate_form()) {
    try {
      isLoading.value = true
      const response = await api.post('/auth/register', form)
      console.log(response.data)
      router.push('/login')
    } catch (e: any) {
      if (!e.response) {
        error.message = 'Сервер не отвечает'
      } else {
        error.status = e.response.status
        if (error.status == 409) {
          error.message = 'Такой email уже существует'
        } else {
          error.message = 'Произошла ошибка'
        }
      }
      console.log('error')
    } finally {
      isLoading.value = false
    }
  }
}
</script>

<template>
  <AuthCard mode="register">
    <form class="auth-form" @submit.prevent="handleRegister">
      <div class="auth-field">
        <label for="name">Имя</label>
        <input v-model="form.name" id="name" type="text" placeholder="Ваше имя" />
        <p v-if="validate_errors.name" class="error validation">{{ validate_errors.name }}</p>
      </div>

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
        {{ isLoading ? 'Загрузка...' : 'Зарегистрироваться' }}
      </button>
      <p v-if="error.message" class="error">{{ error.message }}</p>
    </form>
  </AuthCard>
</template>

<style></style>
