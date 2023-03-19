using Datos;
using Model;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Data
{
    public class PollDTO
    {
        private DataAccess dataAccess;

        public PollDTO()
        {
            dataAccess = new DataAccess();
        }

        public List<Poll> GetPolls(Int32 id)
        {
            List<Poll> polls = new List<Poll>();
            MySqlConnection connection = null;
            try
            {
                User u = new User();
                connection = dataAccess.OpenConnection();
                MySqlCommand command = new MySqlCommand();
                command.Connection = connection;
                command.CommandText = "SELECT * FROM a_polls WHERE p_user_key=@id ORDER BY p_position ASC";
                command.Parameters.AddWithValue("@id", id);
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Poll poll = new Poll();
                        poll.Id = Int32.Parse(reader["p_key"].ToString());
                        poll.Title = reader["p_title"].ToString();
                        poll.Description = reader["p_description"].ToString();
                        poll.Position = Int32.Parse(reader["p_position"].ToString());
                        User user = new User();
                        user.Id = Int32.Parse(reader["p_user_key"].ToString());
                        poll.User = user;
                        polls.Add(poll);
                    }
                }
                return polls;
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

        public Int32 AddPoll(Poll poll)
        {
            MySqlConnection connection = null;
            MySqlTransaction transaction = null;
            try
            {
                connection = dataAccess.OpenConnection();
                transaction = connection.BeginTransaction();
                MySqlCommand command = new MySqlCommand();
                command.Connection = connection;
                command.CommandText = "INSERT INTO a_polls(p_title,p_description,p_position,p_user_key) VALUES(@title,@description,@position,@user_key)";
                command.Parameters.AddWithValue("@title", poll.Title);
                command.Parameters.AddWithValue("@description", poll.Description);
                command.Parameters.AddWithValue("@position", poll.Position);
                command.Parameters.AddWithValue("@user_key", poll.User.Id);
                Int32 result = command.ExecuteNonQuery();
                transaction.Commit();
                return result;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                return 0;
            }
            finally
            {
                dataAccess.CloseConnection(connection);
            }
        }

        public Poll GetPoll(Int32 idUser, Int32 idPoll)
        {
            MySqlConnection connection = null;
            try
            {
                Poll poll = new Poll();
                connection = dataAccess.OpenConnection();
                MySqlCommand command = new MySqlCommand();
                command.Connection = connection;
                command.CommandText = "SELECT * FROM a_polls WHERE p_user_key=@idUser and p_key=@idPoll";
                command.Parameters.AddWithValue("@idUser", idUser);
                command.Parameters.AddWithValue("@idPoll", idPoll);
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        
                        poll.Id = Int32.Parse(reader["p_key"].ToString());
                        poll.Title = reader["p_title"].ToString();
                        poll.Description = reader["p_description"].ToString();
                        poll.Position = Int32.Parse(reader["p_position"].ToString());
                        User user = new User();
                        user.Id = Int32.Parse(reader["p_user_key"].ToString());
                        poll.User = user;
                    }
                }
                return poll;
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

        public Int32 UpdatePoll(Poll poll)
        {
            MySqlConnection connection = null;
            MySqlTransaction transaction = null;
            try
            {
                connection = dataAccess.OpenConnection();
                transaction = connection.BeginTransaction();
                MySqlCommand command = new MySqlCommand();
                command.Connection = connection;
                command.CommandText = "UPDATE a_polls SET p_title=@title,p_description=@description,p_position=@position WHERE p_key=@poll_key and p_user_key=@user_key";
                command.Parameters.AddWithValue("@title", poll.Title);
                command.Parameters.AddWithValue("@description", poll.Description);
                command.Parameters.AddWithValue("@position", poll.Position);
                command.Parameters.AddWithValue("@poll_key", poll.Id);
                command.Parameters.AddWithValue("@user_key", poll.User.Id);
                Int32 result = command.ExecuteNonQuery();
                transaction.Commit();
                return result;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                return 0;
            }
            finally
            {
                dataAccess.CloseConnection(connection);
            }
        }

        public Int32 DeletePoll(Poll poll)
        {
            MySqlConnection connection = null;
            MySqlTransaction transaction = null;
            try
            {
                connection = dataAccess.OpenConnection();
                transaction = connection.BeginTransaction();
                MySqlCommand command = new MySqlCommand();
                command.Connection = connection;
                command.CommandText = "DELETE FROM a_polls WHERE p_key=@poll_key and p_user_key=@user_key";
                command.Parameters.AddWithValue("@poll_key", poll.Id);
                command.Parameters.AddWithValue("@user_key", poll.User.Id);
                Int32 result = command.ExecuteNonQuery();
                transaction.Commit();
                return result;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                return 0;
            }
            finally
            {
                dataAccess.CloseConnection(connection);
            }
        }
    }
}
