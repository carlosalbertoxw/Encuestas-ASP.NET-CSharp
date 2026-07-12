-- Datos de demostración (provenientes del dump original de 2019).
-- Las contraseñas están en formato SHA1 legado a propósito, para probar la
-- migración automática a PBKDF2 al iniciar sesión:
--   c@c.c  -> qwerty
--   a@a.a  -> 123456

INSERT INTO a_users (u_key, u_email, u_password) VALUES
  (2, 'c@c.c', 'b1b3773a05c0ed0176787a4f1574ff0075f7521e'),
  (4, 'a@a.a', '7c4a8d09ca3762af61e59520943dc26494f8941b');

INSERT INTO a_users_profiles (u_p_key, u_p_user_name, u_p_name) VALUES
  (2, 'c', 'C'),
  (4, 'usuario4', 'Usuario4');

INSERT INTO a_polls (p_key, p_title, p_description, p_position, p_user_key) VALUES
  (1, 'Prueba', 'prueba', 1, 2);

INSERT INTO a_answers (a_stars, a_comment, a_poll_key, a_user_key) VALUES
  (4, 'Muy buena encuesta de prueba', 1, 4);
