package main

import (
	"context"
	"log"
	"net/http"
	"os"

	"github.com/NullPointer-analytics-agent/analytics-agent/GoServices/AuthService/internal/application"
	"github.com/NullPointer-analytics-agent/analytics-agent/GoServices/AuthService/internal/repository/postgres"
	"github.com/NullPointer-analytics-agent/analytics-agent/GoServices/AuthService/internal/token"
	authhttp "github.com/NullPointer-analytics-agent/analytics-agent/GoServices/AuthService/internal/transport/http"
	"github.com/jackc/pgx/v5/pgxpool"
)

func main() {
	databaseURL := os.Getenv("DATABASE_URL")
	if databaseURL == "" {
		log.Fatal("DATABASE_URL не задан")
	}
	db, err := pgxpool.New(context.Background(), databaseURL)
	if err != nil {
		log.Fatal(err)
	}
	defer db.Close()
	err = db.Ping(context.Background())
	if err != nil {
		log.Fatal("Ошибка подключения к базе: ", err)
	}
	jwtSecret := os.Getenv("JWT_SECRET")
	tokenManager, err := token.NewJWTManager(jwtSecret)
	if err != nil {
		log.Fatal(err)
	}
	userRepo := postgres.NewUserRepository(db)
	authService := application.NewAuthService(userRepo, tokenManager)
	authHandler := authhttp.NewAuthHandler(authService)
	http.HandleFunc("/api/auth/register", authHandler.Register)
	http.HandleFunc("/api/auth/login", authHandler.Login)
	http.HandleFunc("/api/auth/me", authHandler.Me)
	log.Println("Сервер запущен на порту 8080")
	err = http.ListenAndServe(":8080", nil)
	if err != nil {
		log.Fatal(err)
	}
}
