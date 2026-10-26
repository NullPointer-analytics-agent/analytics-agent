package application

import (
	"context"
	"errors"
	"strings"

	"github.com/NullPointer-analytics-agent/analytics-agent/GoServices/AuthService/internal/domain"
	"github.com/NullPointer-analytics-agent/analytics-agent/GoServices/AuthService/internal/token"
	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"
	"golang.org/x/crypto/bcrypt"
)

type UserRepository interface {
	Create(ctx context.Context, user *domain.User) error
	GetByEmail(ctx context.Context, email string) (*domain.User, error)
	GetByID(ctx context.Context, id uuid.UUID) (*domain.User, error)
}
type AuthService struct {
	repo         UserRepository
	tokenManager *token.JWTManager
}

func NewAuthService(repo UserRepository, tokenManager *token.JWTManager) *AuthService {
	return &AuthService{
		repo:         repo,
		tokenManager: tokenManager,
	}
}
func (s *AuthService) Register(ctx context.Context, name string, email string, password string) (*domain.User, error) {
	name = strings.TrimSpace(name)
	email = strings.TrimSpace(email)
	email = strings.ToLower(email)
	if name == "" || email == "" || password == "" {
		return nil, errors.New("заполните все поля")
	}
	if len(name) > 64 {
		return nil, errors.New("слишком длинное имя")
	}
	if !strings.Contains(email, "@") || len(email) > 254 {
		return nil, errors.New("неправильный email")
	}
	if len(password) < 8 || len(password) > 72 {
		return nil, errors.New("пароль должен быть от 8 до 72 байт")
	}
	hash, err := bcrypt.GenerateFromPassword([]byte(password), bcrypt.DefaultCost)
	if err != nil {
		return nil, err
	}
	user := &domain.User{
		ID:           uuid.New(),
		Username:     name,
		Email:        email,
		PasswordHash: string(hash),
		Role:         "user",
		IsActive:     true,
	}
	err = s.repo.Create(ctx, user)
	if err != nil {
		return nil, err
	}
	return user, nil
}
func (s *AuthService) Login(ctx context.Context, email string, password string) (*domain.User, string, error) {
	email = strings.TrimSpace(email)
	email = strings.ToLower(email)
	user, err := s.repo.GetByEmail(ctx, email)
	if err != nil {
		if errors.Is(err, pgx.ErrNoRows) {
			return nil, "", errors.New("неверный email или пароль")
		}
		return nil, "", err
	}
	if !user.IsActive {
		return nil, "", errors.New("аккаунт отключен")
	}
	err = bcrypt.CompareHashAndPassword([]byte(user.PasswordHash), []byte(password))
	if err != nil {
		return nil, "", errors.New("неверный email или пароль")
	}
	jwtToken, err := s.tokenManager.Generate(user.ID)
	if err != nil {
		return nil, "", err
	}
	return user, jwtToken, nil
}
func (s *AuthService) GetCurrentUser(ctx context.Context, tokenString string) (*domain.User, error) {
	userID, err := s.tokenManager.Verify(tokenString)
	if err != nil {
		return nil, err
	}
	user, err := s.repo.GetByID(ctx, userID)
	if err != nil {
		return nil, err
	}
	if !user.IsActive {
		return nil, errors.New("аккаунт отключен")
	}
	return user, nil
}
