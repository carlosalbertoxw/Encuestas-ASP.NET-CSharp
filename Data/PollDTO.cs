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
    }
}
