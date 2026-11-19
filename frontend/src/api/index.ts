import axios from 'axios'
import { useAuthStore } from '@/stores/auth'
import { useRouter } from 'vue-router'

const router = useRouter()

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:8080/api',
})
export default api

api.interceptors.request.use((config) => {
  const authStore = useAuthStore()
  if (authStore.token) {
    config.headers.Authorization = `Bearer ${authStore.token}`
  }
  return config
})

api.interceptors.response.use(
  (r) => r,
  (error) => {
    const isLoginRequest = error.config?.url?.includes('/auth/login')
    if (error.response?.status === 401 && !isLoginRequest) {
      const authStore = useAuthStore()
      authStore.logout()
      router.push({ name: 'login' })
    }
    return Promise.reject(error)
  },
)
