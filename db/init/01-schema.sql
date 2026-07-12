-- Esquema de la aplicación Encuestas.
-- Modernizado desde el dump original de MySQL 5.7 (DB/Dump20190420.sql, en el historial de git):
--   * utf8mb4 en lugar de utf8 (soporte Unicode completo).
--   * Borrado en cascada: eliminar una cuenta elimina su perfil, encuestas y respuestas.
--   * p_position pasa de TINYINT a INT (la app acepta posiciones de hasta 6 dígitos).
--   * CHECK para restringir la calificación a 1..5 estrellas.

-- Cuentas de acceso.
CREATE TABLE a_users (
  u_key INT NOT NULL AUTO_INCREMENT,
  u_email VARCHAR(50) NOT NULL,
  -- Hash PBKDF2 de ASP.NET Core Identity; las cuentas antiguas pueden traer SHA1
  -- hexadecimal (40 caracteres) y se actualizan automáticamente al iniciar sesión.
  u_password VARCHAR(500) NOT NULL,
  PRIMARY KEY (u_key),
  UNIQUE KEY u_email (u_email)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Perfil público de cada cuenta (relación 1 a 1 con a_users).
CREATE TABLE a_users_profiles (
  u_p_key INT NOT NULL,
  u_p_user_name VARCHAR(25) NOT NULL,
  u_p_name VARCHAR(50) NOT NULL,
  PRIMARY KEY (u_p_key),
  UNIQUE KEY u_p_user (u_p_user_name),
  CONSTRAINT a_users_profiles_ibfk_1 FOREIGN KEY (u_p_key)
    REFERENCES a_users (u_key) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Encuestas creadas por los usuarios.
CREATE TABLE a_polls (
  p_key INT NOT NULL AUTO_INCREMENT,
  p_title VARCHAR(250) NOT NULL,
  p_description VARCHAR(500) NOT NULL DEFAULT '',
  p_position INT NOT NULL,
  p_user_key INT NOT NULL,
  PRIMARY KEY (p_key),
  KEY p_user_key (p_user_key),
  CONSTRAINT a_polls_ibfk_1 FOREIGN KEY (p_user_key)
    REFERENCES a_users_profiles (u_p_key) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Respuestas: calificación de 1 a 5 estrellas y comentario opcional.
CREATE TABLE a_answers (
  a_key INT NOT NULL AUTO_INCREMENT,
  a_stars TINYINT NOT NULL DEFAULT 0,
  a_comment VARCHAR(1000) NOT NULL DEFAULT '',
  a_poll_key INT NOT NULL,
  a_user_key INT NOT NULL,
  PRIMARY KEY (a_key),
  KEY a_poll_key (a_poll_key),
  KEY a_user_key (a_user_key),
  CONSTRAINT a_answers_ibfk_1 FOREIGN KEY (a_poll_key)
    REFERENCES a_polls (p_key) ON DELETE CASCADE,
  CONSTRAINT a_answers_ibfk_2 FOREIGN KEY (a_user_key)
    REFERENCES a_users_profiles (u_p_key) ON DELETE CASCADE,
  CONSTRAINT a_answers_stars_chk CHECK (a_stars BETWEEN 1 AND 5)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
