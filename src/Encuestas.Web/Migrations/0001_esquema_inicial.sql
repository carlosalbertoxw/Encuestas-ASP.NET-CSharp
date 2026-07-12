-- Esquema inicial de la aplicación Encuestas (gestionado por DbUp; ver Infrastructure/MigrationRunner.cs).
-- Características del esquema:
--   * utf8mb4 (soporte Unicode completo) e InnoDB.
--   * Borrado en cascada: eliminar una cuenta elimina su perfil, encuestas y respuestas.
--   * Columnas de auditoría created_at/updated_at.
--   * Sello de seguridad para invalidar sesiones tras un cambio de credenciales.
--   * Confirmación de correo (u_email_confirmed).
--   * Una sola respuesta por usuario y encuesta, con calificación restringida a 1..5.
-- Para evolucionar el esquema en el futuro, agrega un script nuevo (0002_...sql); nunca edites este.

-- Cuentas de acceso.
CREATE TABLE a_users (
  u_key INT NOT NULL AUTO_INCREMENT,
  u_email VARCHAR(50) NOT NULL,
  -- 0 hasta que el usuario confirma su correo mediante el enlace enviado al registrarse.
  u_email_confirmed TINYINT(1) NOT NULL DEFAULT 0,
  -- Hash de la contraseña (PBKDF2 de ASP.NET Core Identity).
  u_password VARCHAR(500) NOT NULL,
  -- VARCHAR (no CHAR(36)) a propósito: MySqlConnector auto-mapea CHAR(36) al tipo Guid y aquí
  -- el sello se maneja como cadena. Lo asigna la aplicación al crear la cuenta.
  u_security_stamp VARCHAR(36) NOT NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (u_key),
  UNIQUE KEY u_email (u_email)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Perfil público de cada cuenta (relación 1 a 1 con a_users).
CREATE TABLE a_users_profiles (
  u_p_key INT NOT NULL,
  u_p_user_name VARCHAR(25) NOT NULL,
  u_p_name VARCHAR(50) NOT NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (u_p_key),
  UNIQUE KEY u_p_user (u_p_user_name),
  CONSTRAINT a_users_profiles_ibfk_1 FOREIGN KEY (u_p_key)
    REFERENCES a_users (u_key) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Encuestas creadas por los usuarios. El índice compuesto sirve al filtro por usuario + orden
-- por posición del tablero y, a la vez, satisface la clave foránea sobre p_user_key.
CREATE TABLE a_polls (
  p_key INT NOT NULL AUTO_INCREMENT,
  p_title VARCHAR(250) NOT NULL,
  p_description VARCHAR(500) NOT NULL DEFAULT '',
  p_position INT NOT NULL,
  p_user_key INT NOT NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (p_key),
  KEY ix_polls_user_position (p_user_key, p_position),
  CONSTRAINT a_polls_ibfk_1 FOREIGN KEY (p_user_key)
    REFERENCES a_users_profiles (u_p_key) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Respuestas: calificación de 1 a 5 estrellas y comentario opcional (inmutables: sin updated_at).
-- Una única respuesta por usuario y encuesta (evita el relleno de urnas).
CREATE TABLE a_answers (
  a_key INT NOT NULL AUTO_INCREMENT,
  a_stars TINYINT NOT NULL DEFAULT 0,
  a_comment VARCHAR(1000) NOT NULL DEFAULT '',
  a_poll_key INT NOT NULL,
  a_user_key INT NOT NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (a_key),
  UNIQUE KEY uq_answer_por_usuario (a_poll_key, a_user_key),
  KEY a_user_key (a_user_key),
  CONSTRAINT a_answers_ibfk_1 FOREIGN KEY (a_poll_key)
    REFERENCES a_polls (p_key) ON DELETE CASCADE,
  CONSTRAINT a_answers_ibfk_2 FOREIGN KEY (a_user_key)
    REFERENCES a_users_profiles (u_p_key) ON DELETE CASCADE,
  CONSTRAINT a_answers_stars_chk CHECK (a_stars BETWEEN 1 AND 5)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
