package token

import (
	"testing"
	"time"

	"github.com/golang-jwt/jwt/v5"
	"github.com/google/uuid"
)

func TestJWT(t *testing.T) {
	manager, err := NewJWTManager("test-secret-key-that-is-long-enough-123")
	if err != nil {
		t.Fatal(err)
	}
	userID := uuid.New()
	jwtToken, err := manager.Generate(userID)
	if err != nil {
		t.Fatal(err)
	}
	resultID, err := manager.Verify(jwtToken)
	if err != nil {
		t.Fatal(err)
	}
	if resultID != userID {
		t.Errorf("ожидали ID %s, получили %s", userID, resultID)
	}
}
func TestInvalidJWT(t *testing.T) {
	manager, err := NewJWTManager("test-secret-key-that-is-long-enough-123")
	if err != nil {
		t.Fatal(err)
	}
	_, err = manager.Verify("wrong-token")
	if err == nil {
		t.Error("ожидали ошибку для неправильного токена")
	}
}
func TestJWTWithWrongSecret(t *testing.T) {
	manager1, err := NewJWTManager("first-secret-key-that-is-long-enough-123")
	if err != nil {
		t.Fatal(err)
	}
	manager2, err := NewJWTManager("second-secret-key-that-is-long-enough-123")
	if err != nil {
		t.Fatal(err)
	}
	userID := uuid.New()
	jwtToken, err := manager1.Generate(userID)
	if err != nil {
		t.Fatal(err)
	}
	_, err = manager2.Verify(jwtToken)
	if err == nil {
		t.Error("токен с чужой подписью не должен проходить проверку")
	}
}
func TestExpiredJWT(t *testing.T) {
	secret := "test-secret-key-that-is-long-enough-123"
	manager, err := NewJWTManager(secret)
	if err != nil {
		t.Fatal(err)
	}
	claims := jwt.RegisteredClaims{
		Subject:   uuid.New().String(),
		ExpiresAt: jwt.NewNumericDate(time.Now().Add(-time.Hour)),
	}
	token := jwt.NewWithClaims(jwt.SigningMethodHS256, claims)
	tokenString, err := token.SignedString([]byte(secret))
	if err != nil {
		t.Fatal(err)
	}
	_, err = manager.Verify(tokenString)
	if err == nil {
		t.Error("просроченный токен не должен проходить проверку")
	}
}
