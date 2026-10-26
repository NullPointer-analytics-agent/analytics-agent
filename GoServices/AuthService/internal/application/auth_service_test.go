package application

import (
	"context"
	"errors"
	"testing"

	"github.com/NullPointer-analytics-agent/analytics-agent/GoServices/AuthService/internal/domain"
	"github.com/NullPointer-analytics-agent/analytics-agent/GoServices/AuthService/internal/token"
	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"
	"golang.org/x/crypto/bcrypt"
)

type FakeUserRepository struct {
	user *domain.User
	err  error
}

func (f *FakeUserRepository) Create(ctx context.Context, user *domain.User) error {
	if f.err != nil {
		return f.err
	}
	f.user = user
	return nil
}
func (f *FakeUserRepository) GetByEmail(ctx context.Context, email string) (*domain.User, error) {
	if f.err != nil {
		return nil, f.err
	}
	if f.user == nil || f.user.Email != email {
		return nil, pgx.ErrNoRows
	}
	return f.user, nil
}
func (f *FakeUserRepository) GetByID(ctx context.Context, id uuid.UUID) (*domain.User, error) {
	if f.err != nil {
		return nil, f.err
	}
	if f.user == nil || f.user.ID != id {
		return nil, pgx.ErrNoRows
	}
	return f.user, nil
}
func TestRegister(t *testing.T) {
	repo := &FakeUserRepository{}
	manager, err := token.NewJWTManager("test-secret-key-that-is-long-enough-123")
	if err != nil {
		t.Fatal(err)
	}
	service := NewAuthService(repo, manager)
	user, err := service.Register(context.Background(), "Margarita", "margarita@example.com", "TestPassword123")
	if err != nil {
		t.Fatal(err)
	}
	if user.Username != "Margarita" {
		t.Error("неправильное имя")
	}
	if user.Email != "margarita@example.com" {
		t.Error("неправильный email")
	}
	if repo.user == nil {
		t.Fatal("пользователь не сохранился")
	}
	if user.PasswordHash == "TestPassword123" {
		t.Error("пароль не захеширован")
	}
	err = bcrypt.CompareHashAndPassword([]byte(user.PasswordHash), []byte("TestPassword123"))
	if err != nil {
		t.Error("неправильно захеширован пароль")
	}
}
func TestRegisterInvalidData(t *testing.T) {
	repo := &FakeUserRepository{}
	manager, err := token.NewJWTManager("test-secret-key-that-is-long-enough-123")
	if err != nil {
		t.Fatal(err)
	}
	service := NewAuthService(repo, manager)
	tests := []struct {
		name     string
		username string
		email    string
		password string
	}{
		{"пустое имя", "", "test@example.com", "TestPassword123"},
		{"неправильный email", "Margarita", "wrong-email", "TestPassword123"},
		{"короткий пароль", "Margarita", "test@example.com", "123"},
		{"пустой пароль", "Margarita", "test@example.com", ""},
	}
	for _, test := range tests {
		t.Run(test.name, func(t *testing.T) {
			_, err := service.Register(context.Background(), test.username, test.email, test.password)
			if err == nil {
				t.Error("должна быть ошибка")
			}
		})
	}
	if repo.user != nil {
		t.Error("пользователь не должен был сохраниться")
	}
}

func TestLogin(t *testing.T) {
	repo := &FakeUserRepository{}
	manager, err := token.NewJWTManager("test-secret-key-that-is-long-enough-123")
	if err != nil {
		t.Fatal(err)
	}
	service := NewAuthService(repo, manager)
	user, err := service.Register(context.Background(), "Margarita", "margarita@example.com", "TestPassword123")
	if err != nil {
		t.Fatal(err)
	}
	loginUser, jwtToken, err := service.Login(context.Background(), "margarita@example.com", "TestPassword123")
	if err != nil {
		t.Fatal(err)
	}
	if loginUser.ID != user.ID {
		t.Error("не совпадает ID пользователя")
	}
	if jwtToken == "" {
		t.Fatal("токен пустой")
	}
	id, err := manager.Verify(jwtToken)
	if err != nil {
		t.Fatal(err)
	}
	if id != user.ID {
		t.Error("неправильный ID в токене")
	}
}
func TestLoginWrongPassword(t *testing.T) {
	repo := &FakeUserRepository{}
	manager, err := token.NewJWTManager("test-secret-key-that-is-long-enough-123")
	if err != nil {
		t.Fatal(err)
	}
	service := NewAuthService(repo, manager)
	_, err = service.Register(context.Background(), "Margarita", "margarita@example.com", "TestPassword123")
	if err != nil {
		t.Fatal(err)
	}
	_, jwtToken, err := service.Login(context.Background(), "margarita@example.com", "WrongPassword123")

	if err == nil {
		t.Error("должна быть ошибка при неправильном пароле")
	}
	if jwtToken != "" {
		t.Error("токен не должен выдаваться")
	}
}
func TestLoginUnknownUser(t *testing.T) {
	repo := &FakeUserRepository{}
	manager, err := token.NewJWTManager("test-secret-key-that-is-long-enough-123")
	if err != nil {
		t.Fatal(err)
	}
	service := NewAuthService(repo, manager)
	_, jwtToken, err := service.Login(context.Background(), "unknown@example.com", "TestPassword123")
	if err == nil {
		t.Error("несуществующий пользователь не должен войти")
	}
	if jwtToken != "" {
		t.Error("токен не должен выдаваться")
	}
}
func TestLoginInactiveUser(t *testing.T) {
	repo := &FakeUserRepository{}
	manager, err := token.NewJWTManager("test-secret-key-that-is-long-enough-123")
	if err != nil {
		t.Fatal(err)
	}
	service := NewAuthService(repo, manager)
	_, err = service.Register(context.Background(), "Margarita", "margarita@example.com", "TestPassword123")
	if err != nil {
		t.Fatal(err)
	}
	repo.user.IsActive = false
	_, jwtToken, err := service.Login(context.Background(), "margarita@example.com", "TestPassword123")
	if err == nil {
		t.Error("отключённый пользователь не должен войти")
	}
	if jwtToken != "" {
		t.Error("токен не должен выдаваться")
	}
}
func TestLoginDatabaseError(t *testing.T) {
	repo := &FakeUserRepository{}
	repo.err = errors.New("database unavailable")
	manager, err := token.NewJWTManager("test-secret-key-that-is-long-enough-123")
	if err != nil {
		t.Fatal(err)
	}
	service := NewAuthService(repo, manager)
	_, _, err = service.Login(context.Background(), "margarita@example.com", "TestPassword123")
	if err == nil {
		t.Fatal("должна быть ошибка базы данных")
	}
	if err.Error() == "неверный email или пароль" {
		t.Error("ошибка базы данных не должна считаться неправильным паролем")
	}
}
func TestGetCurrentUser(t *testing.T) {
	repo := &FakeUserRepository{}
	manager, err := token.NewJWTManager("test-secret-key-that-is-long-enough-123")
	if err != nil {
		t.Fatal(err)
	}
	service := NewAuthService(repo, manager)
	user, err := service.Register(context.Background(), "Margarita", "margarita@example.com", "TestPassword123")
	if err != nil {
		t.Fatal(err)
	}
	_, jwtToken, err := service.Login(context.Background(), "margarita@example.com", "TestPassword123")
	if err != nil {
		t.Fatal(err)
	}
	currentUser, err := service.GetCurrentUser(context.Background(), jwtToken)
	if err != nil {
		t.Fatal(err)
	}
	if currentUser.ID != user.ID {
		t.Error("неправильный пользователь")
	}
}
func TestGetCurrentUserInvalidToken(t *testing.T) {
	repo := &FakeUserRepository{}
	manager, err := token.NewJWTManager("test-secret-key-that-is-long-enough-123")
	if err != nil {
		t.Fatal(err)
	}
	service := NewAuthService(repo, manager)
	_, err = service.GetCurrentUser(context.Background(), "wrong-token")
	if err == nil {
		t.Error("неправильный токен должен вызывать ошибку")
	}
}
