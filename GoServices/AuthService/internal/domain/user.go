package domain

import (
	"time"

	"github.com/google/uuid"
)

type User struct {
	ID           uuid.UUID
	Username     string
	Email        string
	PasswordHash string
	Role         string
	IsActive     bool
	CreatedAt    time.Time
	UpdatedAt    time.Time
	LastLoginAt  *time.Time
}
