-- * Bandeja de salida de correos (outbox): las peticiones solo insertan el mensaje y un
--   proceso en segundo plano lo envía con reintentos. Si el SMTP falla, la petición no se
--   rompe y el correo sale cuando el servidor vuelve. Los enviados se borran.
-- * Contadores de ventana fija para el rate limiting y el bloqueo por cuenta, compartidos
--   entre réplicas (antes vivían en la memoria de cada proceso).

CREATE TABLE a_outbox (
  o_key BIGINT NOT NULL AUTO_INCREMENT,
  o_recipient VARCHAR(254) NOT NULL,
  o_subject VARCHAR(200) NOT NULL,
  o_body MEDIUMTEXT NOT NULL,
  o_attempts INT NOT NULL DEFAULT 0,
  -- UTC; el siguiente intento no se hace antes de esta fecha (espera exponencial).
  o_next_attempt_at DATETIME NOT NULL,
  -- UTC; se rellena cuando se agotan los reintentos y el mensaje se da por perdido.
  o_failed_at DATETIME NULL,
  o_last_error VARCHAR(200) NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (o_key),
  KEY ix_outbox_pending (o_failed_at, o_next_attempt_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE a_attempt_counters (
  ac_key VARCHAR(191) NOT NULL,
  ac_count INT NOT NULL DEFAULT 0,
  -- UTC; inicio de la ventana vigente.
  ac_window_start DATETIME NOT NULL,
  PRIMARY KEY (ac_key),
  KEY ix_attempt_counters_window (ac_window_start)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
