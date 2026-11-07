-- * Amplía los correos a 254 caracteres (el máximo que admite una dirección válida).
-- * Correo pendiente: un cambio de correo solo se aplica cuando se confirma desde la
--   dirección nueva; mientras tanto la cuenta sigue usando la anterior.
-- * Sesiones en el servidor (ITicketStore): la cookie solo guarda un identificador, y al
--   cerrar sesión o borrar la cuenta la sesión deja de existir aunque alguien copie la cookie.

ALTER TABLE a_users
  MODIFY u_email VARCHAR(254) NOT NULL,
  ADD COLUMN u_pending_email VARCHAR(254) NULL AFTER u_email_confirmed;

CREATE TABLE a_sessions (
  s_key VARCHAR(64) NOT NULL,
  s_user_key INT NOT NULL,
  -- Ticket de autenticación serializado (claims y propiedades de la sesión).
  s_ticket MEDIUMBLOB NOT NULL,
  -- UTC; NULL si el ticket no caduca.
  s_expires_at DATETIME NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (s_key),
  KEY ix_sessions_user (s_user_key),
  KEY ix_sessions_expires (s_expires_at),
  CONSTRAINT a_sessions_ibfk_1 FOREIGN KEY (s_user_key)
    REFERENCES a_users (u_key) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
