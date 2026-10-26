# AuthService
Сервис регистрации и авторизации пользователей на Go.
Используется PostgreSQL для хранения пользователей и JWT для авторизации.

## Возможности
- Регистрация пользователей
- Авторизация по email и паролю
- Хеширование паролей с помощью bcrypt
- Создание и проверка JWT
- Получение информации о текущем пользователе

## Запуск
### 1. Запустить PostgreSQL
```bash
docker run -d \
  --name analytics-auth-db \
  -e POSTGRES_USER=auth_user \
  -e POSTGRES_PASSWORD=auth_local_dev_password \
  -e POSTGRES_DB=auth_db \
  -p 127.0.0.1:5433:5432 \
  -v analytics_auth_data:/var/lib/postgresql/data \
  postgres:17
```
Если контейнер уже существует, его можно запустить:
```bash
docker start analytics-auth-db
```
### 2. Создать таблицу пользователей
Выполнить SQL из файла:
`migrations/000001_create_users.up.sql`
Например:
```bash
docker exec -i analytics-auth-db \
  psql -U auth_user -d auth_db \
  < migrations/000001_create_users.up.sql
```

### 3. Настроить переменные окружения
```bash
export DATABASE_URL="postgres://auth_user:auth_local_dev_password@127.0.0.1:5433/auth_db?sslmode=disable"
export JWT_SECRET="$(openssl rand -hex 32)"
```

### 4. Запустить сервер
```bash
go run ./cmd/server
```
Сервис запускается на порту `8080`.
## API
### POST /api/auth/register
Регистрация пользователя.
Пример запроса:
```json
{
  "name": "Margarita",
  "email": "margarita@example.com",
  "password": "TestPassword123"
}
```
Успешный ответ: `201 Created`.

### POST /api/auth/login
Авторизация пользователя.
Пример запроса:
```json
{
  "email": "margarita@example.com",
  "password": "TestPassword123"
}
```
Успешный ответ: `200 OK`.
Ответ содержит данные пользователя и JWT-токен.
### GET /api/auth/me
Получение информации о текущем пользователе.
Необходимо передать JWT в заголовке:
```http
Authorization: Bearer <token>
```
Успешный ответ: `200 OK`.
## Тестирование
Запуск всех тестов:

```bash
go test ./...
```
Дополнительная проверка:

```bash
go vet ./...
```

## Структура проекта

- `cmd/server` — запуск сервера
- `internal/domain` — модель пользователя
- `internal/application` — логика регистрации и авторизации
- `internal/repository/postgres` — работа с PostgreSQL
- `internal/token` — JWT
- `internal/transport/http` — HTTP-обработчики
- `migrations` — SQL-миграции