package http

import (
	"encoding/json"
	"errors"
	"log"
	"net/http"
	"strings"

	"github.com/NullPointer-analytics-agent/analytics-agent/GoServices/AuthService/internal/application"
	"github.com/jackc/pgx/v5/pgconn"
)

type AuthHandler struct {
	service *application.AuthService
}

func NewAuthHandler(service *application.AuthService) *AuthHandler {
	return &AuthHandler{service: service}
}

type RegisterRequest struct {
	Name     string `json:"name"`
	Email    string `json:"email"`
	Password string `json:"password"`
}
type RegisterResponse struct {
	ID    string `json:"id"`
	Name  string `json:"name"`
	Email string `json:"email"`
}

func (h *AuthHandler) Register(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		http.Error(w, "метод не поддерживается", http.StatusMethodNotAllowed)
		return
	}
	var request RegisterRequest
	err := json.NewDecoder(r.Body).Decode(&request)
	if err != nil {
		http.Error(w, "неправильный JSON", http.StatusBadRequest)
		return
	}
	user, err := h.service.Register(
		r.Context(),
		request.Name,
		request.Email,
		request.Password,
	)
	if err != nil {
		var pgErr *pgconn.PgError
		if errors.As(err, &pgErr) && pgErr.Code == "23505" {
			http.Error(w, "пользователь с таким email уже существует", http.StatusConflict)
			return
		}
		message := err.Error()
		if strings.Contains(message, "заполните все поля") || strings.Contains(message, "слишком длинное имя") || strings.Contains(message, "неправильный email") || strings.Contains(message, "пароль должен быть") {
			http.Error(w, message, http.StatusBadRequest)
			return
		}
		log.Println("Ошибка регистрации:", err)
		http.Error(w, "внутренняя ошибка сервера", http.StatusInternalServerError)
		return
	}

	response := RegisterResponse{
		ID:    user.ID.String(),
		Name:  user.Username,
		Email: user.Email,
	}
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(http.StatusCreated)
	json.NewEncoder(w).Encode(response)
}

type LoginRequest struct {
	Email    string `json:"email"`
	Password string `json:"password"`
}
type LoginResponse struct {
	ID    string `json:"id"`
	Name  string `json:"name"`
	Email string `json:"email"`
	Token string `json:"token"`
}

func (h *AuthHandler) Login(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		http.Error(w, "метод не поддерживается", http.StatusMethodNotAllowed)
		return
	}
	var request LoginRequest
	err := json.NewDecoder(r.Body).Decode(&request)
	if err != nil {
		http.Error(w, "неправильный JSON", http.StatusBadRequest)
		return
	}
	if request.Email == "" || request.Password == "" {
		http.Error(w, "заполните все поля", http.StatusBadRequest)
		return
	}
	user, jwtToken, err := h.service.Login(r.Context(), request.Email, request.Password)
	if err != nil {
		if err.Error() == "аккаунт отключен" {
			http.Error(w, "аккаунт отключен", http.StatusForbidden)
			return
		}
		if err.Error() == "неверный email или пароль" {
			http.Error(w, "неверный email или пароль", http.StatusUnauthorized)
			return
		}
		log.Println("Ошибка входа:", err)
		http.Error(w, "внутренняя ошибка сервера", http.StatusInternalServerError)
		return
	}
	response := LoginResponse{
		ID:    user.ID.String(),
		Name:  user.Username,
		Email: user.Email,
		Token: jwtToken,
	}
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(http.StatusOK)
	json.NewEncoder(w).Encode(response)
}
func (h *AuthHandler) Me(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		http.Error(w, "метод не поддерживается", http.StatusMethodNotAllowed)
		return
	}
	authHeader := r.Header.Get("Authorization")
	if !strings.HasPrefix(authHeader, "Bearer ") {
		http.Error(w, "токен не передан", http.StatusUnauthorized)
		return
	}
	tokenString := strings.TrimPrefix(authHeader, "Bearer ")
	user, err := h.service.GetCurrentUser(r.Context(), tokenString)
	if err != nil {
		http.Error(w, "доступ запрещён", http.StatusUnauthorized)
		return
	}
	response := MeResponse{
		ID:    user.ID.String(),
		Name:  user.Username,
		Email: user.Email,
	}
	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(response)
}

type MeResponse struct {
	ID    string `json:"id"`
	Name  string `json:"name"`
	Email string `json:"email"`
}
