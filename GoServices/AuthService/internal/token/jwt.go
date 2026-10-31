package token

import (
	"errors"
	"time"

	"github.com/golang-jwt/jwt/v5"
	"github.com/google/uuid"
)

type JWTManager struct {
	secret []byte
}

func NewJWTManager(secret string) (*JWTManager, error) {
	if len(secret) < 32 {
		return nil, errors.New("JWT_SECRET должен содержать минимум 32 байта")
	}
	return &JWTManager{
		secret: []byte(secret),
	}, nil
}
func (m *JWTManager) Generate(userID uuid.UUID) (string, error) {
	claims := jwt.RegisteredClaims{
		Subject:   userID.String(),
		ExpiresAt: jwt.NewNumericDate(time.Now().Add(24 * time.Hour)),
		IssuedAt:  jwt.NewNumericDate(time.Now()),
	}
	t := jwt.NewWithClaims(jwt.SigningMethodHS256, claims)
	return t.SignedString(m.secret)
}
func (m *JWTManager) Verify(tokenString string) (uuid.UUID, error) {
	claims := &jwt.RegisteredClaims{}
	parsedToken, err := jwt.ParseWithClaims(
		tokenString,
		claims,
		func(t *jwt.Token) (interface{}, error) {
			if t.Method != jwt.SigningMethodHS256 {
				return nil, errors.New("неподдерживаемый алгоритм подписи")
			}
			return m.secret, nil
		},
		jwt.WithExpirationRequired(),
	)
	if err != nil || !parsedToken.Valid {
		return uuid.Nil, errors.New("недействительный токен")
	}
	userID, err := uuid.Parse(claims.Subject)
	if err != nil {
		return uuid.Nil, errors.New("неправильный ID пользователя в токене")
	}
	return userID, nil
}
