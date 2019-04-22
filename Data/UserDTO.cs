using Datos;
using Model;
using MySql.Data.MySqlClient;
using System;
using System.Diagnostics;

namespace Data
{
    public class UserDTO
    {
        private DataAccess dataAccess;

        public UserDTO()
        {
            dataAccess = new DataAccess();
        }

        public bool EditProfile(UserProfile up)
        {
            MySqlConnection connection = null;
            MySqlTransaction transaction = null;
            try
            {
                bool r;
                connection = dataAccess.OpenConnection();
                transaction = connection.BeginTransaction();
                MySqlCommand command = new MySqlCommand();
                command.Connection = connection;
                command.CommandText = "UPDATE a_users_profiles SET u_p_name=@Name WHERE u_p_key=@id";
                command.Parameters.AddWithValue("@id", up.User.Id);
                command.Parameters.AddWithValue("@Name", up.Name);
                Int32 result = command.ExecuteNonQuery();

                if (result == 1)
                {
                    r = true;
                }
                else
                {
                    r = false;
                }
                transaction.Commit();
                return r;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                return false;
            }
            finally
            {
                dataAccess.CloseConnection(connection);
            }
        }

        public bool ChangeUser(UserProfile up)
        {
            MySqlConnection connection = null;
            MySqlTransaction transaction = null;
            try
            {
                bool r;
                connection = dataAccess.OpenConnection();
                transaction = connection.BeginTransaction();
                MySqlCommand command = new MySqlCommand();
                command.Connection = connection;
                command.CommandText = "UPDATE a_users_profiles SET u_p_user_name=@userName WHERE u_p_key=@id";
                command.Parameters.AddWithValue("@id", up.User.Id);
                command.Parameters.AddWithValue("@userName", up.UserName);
                Int32 result = command.ExecuteNonQuery();

                if (result == 1)
                {
                    r = true;
                }
                else
                {
                    r = false;
                }
                transaction.Commit();
                return r;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                return false;
            }
            finally
            {
                dataAccess.CloseConnection(connection);
            }
        }

        public bool ChangeEmail(User u)
        {
            MySqlConnection connection = null;
            MySqlTransaction transaction = null;
            try
            {
                bool r;
                connection = dataAccess.OpenConnection();
                transaction = connection.BeginTransaction();
                MySqlCommand command = new MySqlCommand();
                command.Connection = connection;
                command.CommandText = "UPDATE a_users SET u_email=@email WHERE u_key=@id";
                command.Parameters.AddWithValue("@id", u.Id);
                command.Parameters.AddWithValue("@email", u.Email);
                Int32 result = command.ExecuteNonQuery();

                if (result == 1)
                {
                    r = true;
                }
                else
                {
                    r = false;
                }
                transaction.Commit();
                return r;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                return false;
            }
            finally
            {
                dataAccess.CloseConnection(connection);
            }
        }

        public bool ChangePassword(User u)
        {
            MySqlConnection connection = null;
            MySqlTransaction transaction = null;
            try
            {
                bool r;
                connection = dataAccess.OpenConnection();
                transaction = connection.BeginTransaction();
                MySqlCommand command = new MySqlCommand();
                command.Connection = connection;
                command.CommandText = "UPDATE a_users SET u_password=@password WHERE u_key=@id";
                command.Parameters.AddWithValue("@id", u.Id);
                command.Parameters.AddWithValue("@password", u.Password);
                Int32 result = command.ExecuteNonQuery();

                if (result == 1)
                {
                    r = true;
                }
                else
                {
                    r = false;
                }
                transaction.Commit();
                return r;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                return false;
            }
            finally
            {
                dataAccess.CloseConnection(connection);
            }
        }

        public bool DeleteAccount(Int32 id)
        {
            MySqlConnection connection = null;
            MySqlTransaction transaction = null;
            try
            {
                bool r;
                connection = dataAccess.OpenConnection();
                transaction = connection.BeginTransaction();
                MySqlCommand command = new MySqlCommand();
                command.Connection = connection;
                command.CommandText = "DELETE FROM a_users_profiles WHERE u_p_key=@id";
                command.Parameters.AddWithValue("@id", id);
                Int32 result = command.ExecuteNonQuery();
                command.Parameters.Clear();
                command.CommandText = "DELETE FROM a_users WHERE u_key=@id";
                command.Parameters.AddWithValue("@id", id);
                Int32 result2 = command.ExecuteNonQuery();

                if (result == 1 && result2 == 1)
                {
                    r = true;
                }
                else
                {
                    r = false;
                }
                transaction.Commit();
                return r;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                return false;
            }
            finally
            {
                dataAccess.CloseConnection(connection);
            }
        }

        public User GetUserPassword(Int32 id)
        {
            MySqlConnection connection = null;
            try
            {
                User u = new User();
                connection = dataAccess.OpenConnection();
                MySqlCommand command = new MySqlCommand();
                command.Connection = connection;
                command.CommandText = "SELECT * FROM a_users WHERE u_key=@id";
                command.Parameters.AddWithValue("@id", id);
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        u.Id = Int32.Parse(reader["u_key"].ToString());
                        u.Password = reader["u_password"].ToString();
                    }
                }
                return u;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                return null;
            }
            finally
            {
                dataAccess.CloseConnection(connection);
            }
        }

        public UserProfile Profile(String userName)
        {
            MySqlConnection connection = null;
            try
            {
                UserProfile up = new UserProfile();
                connection = dataAccess.OpenConnection();
                MySqlCommand command = new MySqlCommand();
                command.Connection = connection;
                command.CommandText = "SELECT * FROM a_users_profiles WHERE u_p_user_name=@userName";
                command.Parameters.AddWithValue("@userName", userName);
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        User u = new User();
                        u.Id = Int32.Parse(reader["u_p_key"].ToString());
                        up.User = u;
                        up.UserName = reader["u_p_user_name"].ToString();
                        up.Name = reader["u_p_name"].ToString();
                    }
                }
                return up;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                return null;
            }
            finally
            {
                dataAccess.CloseConnection(connection);
            }
        }

        public bool addUser(String email, String password)
        {
            MySqlConnection connection = null;
            MySqlTransaction transaction = null;
            try
            {
                bool r;
                connection = dataAccess.OpenConnection();
                transaction = connection.BeginTransaction();
                MySqlCommand command = new MySqlCommand();
                command.Connection = connection;
                command.CommandText = "INSERT INTO a_users(u_email,u_password) VALUES(@email,@password)";
                command.Parameters.AddWithValue("@email", email);
                command.Parameters.AddWithValue("@password", password);
                Int32 result = command.ExecuteNonQuery();

                Int32 id=0;
                command.CommandText = "select max(u_key) last_key from a_users";
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        id = Int32.Parse(reader["last_key"].ToString());
                    }
                }

                command.CommandText = "INSERT INTO a_users_profiles(u_p_key,u_p_user_name,u_p_name) VALUES(@id,@userName,@name)";
                command.Parameters.AddWithValue("@id", id);
                command.Parameters.AddWithValue("@userName", "usuario"+id);
                command.Parameters.AddWithValue("@name", "Usuario"+id);
                Int32 result2 = command.ExecuteNonQuery();

                if (result == 1 && result2 == 1)
                {
                    r = true;
                }
                else
                {
                    r = false;
                }
                transaction.Commit();
                return r;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                return false;
            }
            finally
            {
                dataAccess.CloseConnection(connection);
            }
        }

        public UserProfile GetUserProfileByEmail(String email)
        {
            MySqlConnection connection = null;
            try
            {
                UserProfile up = new UserProfile();
                connection = dataAccess.OpenConnection();
                MySqlCommand command = new MySqlCommand();
                command.Connection = connection;
                command.CommandText = "SELECT * FROM a_users AS u JOIN a_users_profiles AS up ON u.u_key=up.u_p_key WHERE u.u_email=@email";
                command.Parameters.AddWithValue("@email", email);
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        User u = new User();
                        u.Id = Int32.Parse(reader["u_key"].ToString());
                        u.Email = reader["u_email"].ToString();
                        u.Password = reader["u_password"].ToString();
                        up.User = u;
                        up.UserName = reader["u_p_user_name"].ToString();
                        up.Name = reader["u_p_name"].ToString();
                    }
                }
                return up;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                return null;
            }
            finally
            {
                dataAccess.CloseConnection(connection);
            }
        }
    }
}
