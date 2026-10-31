<template>
  <div class="home-page">
    <header class="header">
      <div class="header-left">
        <button type="button" class="menu-button" aria-label="Открыть меню">
          <span></span>
          <span></span>
          <span></span>
        </button>

        <h1 class="logo">Агент-аналитик</h1>
      </div>

      <div class="header-actions">
        <RouterLink to="/login" class="login-button">Войти</RouterLink>
        <RouterLink to="/register" class="register-button">Регистрация</RouterLink>
      </div>
    </header>

    <main class="main-content">
      <section class="hero">
        <p class="hero-label">Проверка требований перебором</p>

        <h2 class="hero-title">
          Находим логические<br />
          ошибки в требованиях
        </h2>

        <p class="hero-description">
          Опишите правила системы обычным текстом. Агент построит модель,<br
            class="desktop-break"
          />
          проверит все сценарии и покажет, где они ломаются.
        </p>
      </section>

      <!-- Карточка для ввода требований -->
      <section class="requirements-card">
        <form class="requirements-form">
          <label for="requirements" class="requirements-label"> Ваши требования </label>

          <textarea
            id="requirements"
            class="requirements-input"
            placeholder="Заявку можно согласовать, исполнить или отменить до исполнения..."
          ></textarea>

          <div class="form-bottom">
            <p class="form-hint">Чтобы сохранять диалоги, войдите в аккаунт</p>

            <button type="button" class="check-button">Проверить требования</button>
          </div>
        </form>
      </section>
      <section class="steps">
        <div class="step">
          <span class="step-number">1</span>
          <p class="step-text">Опишите правила</p>
        </div>

        <div class="step">
          <span class="step-number">2</span>
          <p class="step-text">Агент строит модель</p>
        </div>

        <div class="step">
          <span class="step-number">3</span>
          <p class="step-text">Получите сценарий</p>
        </div>
      </section>
    </main>
  </div>
</template>

<style scoped>
.home-page {
  box-sizing: border-box;
  width: 100%;
  min-height: 100vh;
  padding: 0;
  background-color: #f4f6f3;
  color: #19231f;
  font-family: Arial, sans-serif;
}

.header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  height: 74px;
  padding: 0 42px;
  background-color: #ffffff;
  border-bottom: 1px solid #dce2dd;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 16px;
}

.menu-button {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 5px;
  width: 46px;
  height: 46px;
  padding: 0;
  background-color: #ffffff;
  border: 1px solid #cbd5ce;
  border-radius: 12px;
  cursor: pointer;
}

.menu-button span {
  display: block;
  width: 18px;
  height: 2px;
  background-color: #19231f;
  border-radius: 2px;
}

.logo {
  margin: 0;
  color: #19231f;
  font-size: 22px;
  font-weight: 700;
}

.header-actions {
  display: flex;
  align-items: center;
  gap: 12px;
}

.login-button,
.register-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-height: 48px;
  padding: 0 20px;
  border-radius: 12px;
  font-size: 16px;
  text-decoration: none;
  transition:
    background-color 0.2s,
    color 0.2s;
}

.login-button {
  color: #19231f;
  background-color: #ffffff;
  border: 1px solid #cbd5ce;
}

.login-button:hover {
  background-color: #f1f5f2;
}

.register-button {
  color: #ffffff;
  background-color: #0f806d;
  border: 1px solid #0f806d;
}

.register-button:hover {
  background-color: #0b6c5c;
}

.main-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  min-height: calc(100vh - 74px);
  padding: 38px 24px 24px;
  background-color: #f4f6f3;
}

.hero {
  width: 100%;
  max-width: 850px;
  text-align: center;
}

.hero-label {
  display: inline-block;
  margin: 0 0 26px;
  padding: 6px 12px;
  border-radius: 20px;
  background-color: #e2f3ed;
  color: #076b5b;
  font-size: 13px;
  line-height: 1.4;
}

.hero-title {
  margin: 0;
  color: #19231f;
  font-size: 56px;
  font-weight: 700;
  line-height: 1.08;
  letter-spacing: -1.5px;
}

.hero-description {
  margin: 26px 0 0;
  color: #697670;
  font-size: 20px;
  line-height: 1.5;
}

.requirements-card {
  box-sizing: border-box;
  width: 100%;
  max-width: 798px;
  margin-top: 26px;
  padding: 24px 22px 22px;
  background-color: #ffffff;
  border: 1px solid #d5ded8;
  border-radius: 17px;
}

.requirements-form {
  display: flex;
  flex-direction: column;
}

.requirements-label {
  margin-bottom: 10px;
  color: #52615a;
  font-size: 14px;
}

.requirements-input {
  box-sizing: border-box;
  width: 100%;
  min-height: 96px;
  padding: 14px 13px;
  resize: vertical;
  background-color: #ffffff;
  border: 1px solid #cbd5ce;
  border-radius: 11px;
  color: #19231f;
  font-family: inherit;
  font-size: 15px;
  line-height: 1.5;
  outline: none;
  transition:
    border-color 0.2s,
    box-shadow 0.2s;
}

.requirements-input::placeholder {
  color: #858b87;
}

.requirements-input:focus {
  border-color: #0f806d;
  box-shadow: 0 0 0 3px rgba(15, 128, 109, 0.12);
}

.form-bottom {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 20px;
  margin-top: 20px;
}

.form-hint {
  margin: 0;
  color: #697670;
  font-size: 13px;
  line-height: 1.5;
}

.check-button {
  flex-shrink: 0;
  min-height: 48px;
  padding: 0 20px;
  background-color: #0f806d;
  border: 1px solid #0f806d;
  border-radius: 11px;
  color: #ffffff;
  font-family: inherit;
  font-size: 16px;
  cursor: pointer;
  transition: background-color 0.2s;
}

.check-button:hover {
  background-color: #0b6c5c;
}

.steps {
  display: flex;
  align-items: center;
  justify-content: center;
  flex-wrap: wrap;
  gap: 40px;
  margin-top: 25px;
}

.step {
  display: flex;
  align-items: center;
  gap: 10px;
}

.step-number {
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  width: 30px;
  height: 30px;
  border-radius: 50%;
  background-color: #19231f;
  color: #ffffff;
  font-size: 14px;
}

.step-text {
  margin: 0;
  color: #19231f;
  font-size: 15px;
  font-weight: 500;
}

@media (max-width: 700px) {
  .home-page {
    padding: 8px;
  }

  .header {
    min-height: 68px;
    padding: 0 14px;
  }

  .header-left {
    gap: 10px;
  }

  .logo {
    font-size: 17px;
  }

  .menu-button {
    width: 40px;
    height: 40px;
  }

  .header-actions {
    gap: 6px;
  }

  .login-button,
  .register-button {
    min-height: 40px;
    padding: 0 10px;
    border-radius: 9px;
    font-size: 13px;
  }

  .main-content {
    min-height: calc(100vh - 84px);
    padding: 42px 12px 30px;
  }

  .hero-label {
    margin-bottom: 20px;
    font-size: 12px;
  }

  .hero-title {
    font-size: 36px;
    letter-spacing: -0.7px;
  }

  .hero-description {
    margin-top: 18px;
    font-size: 16px;
  }

  .desktop-break {
    display: none;
  }

  .requirements-card {
    margin-top: 24px;
    padding: 18px 15px;
  }

  .form-bottom {
    align-items: stretch;
    flex-direction: column;
    gap: 14px;
  }

  .check-button {
    width: 100%;
  }

  .steps {
    align-items: flex-start;
    flex-direction: column;
    gap: 16px;
    margin-top: 24px;
  }
}
</style>
