
<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import HomeView from '@/views/HomeView.vue'

const route = useRoute()
const router = useRouter()

const isAuthPage = computed(
  () => route.path === '/login' || route.path === '/register',
)

function closeAuth() {
  router.push('/')
}
</script>

<template>
  <template v-if="isAuthPage">
    <HomeView />
    <div class="home-dimmed" @click="closeAuth"></div>
    <div class="home-auth-drawer">
      <RouterView />
    </div>
  </template>
  <RouterView v-else />
</template>

<style scoped>
.home-dimmed {
  position: fixed;
  inset: 0;
  z-index: 1000;
  background-color: rgba(0, 0, 0, 0.4);
}

.home-auth-drawer {
  position: fixed;
  top: 0;
  right: 0;
  z-index: 1001;
  width: 575px;
  max-width: 100%;
  height: 100dvh;
  overflow-y: auto;
  background-color: #ffffff;
  box-shadow: -8px 0 28px rgba(0, 0, 0, 0.08);

  animation: drawer-slide-in 0.3s ease-out;
}

@keyframes drawer-slide-in {
  from {
    transform: translateX(100%);
  }

  to {
    transform: translateX(0);
  }
}

@media (prefers-reduced-motion: reduce) {
  .home-auth-drawer {
    animation: none;
  }
}
</style>
