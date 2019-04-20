using System;
using MySql.Data.MySqlClient;

namespace Datos
{
    class DataAccess
    {
        private String host = "localhost";
        private String dbname = "application";
        private String user = "root";
        private String password = "qwerty";

        public MySqlConnection OpenConnection()
        {
            MySqlConnection connection;
            try
            {
                connection = new MySqlConnection("Server=" + host + "; Database=" + dbname + "; User id=" + user + "; Pwd=" + password);
                connection.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ha surgido un error y no se puede abrir la conexión a la base de datos. Detalles: " + ex.ToString());
                connection = null;
            }

            return connection;
        }

        public void CloseConnection(MySqlConnection connection)
        {
            try
            {
                connection.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ha surgido un error y no se puede cerrar la conexión a la base de datos. Detalles: " + ex.ToString());
                connection = null;
            }
        }
    }
}
