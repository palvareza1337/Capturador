using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;


namespace Capturador
{
    internal class SqlConnection
    {
        private readonly MySqlConnection connection;

        public SqlConnection(string connectionString)
        {
            connection = new MySqlConnection(connectionString);
            connection.Open();
        }



        //----------------------------------------

        public SqlConnection(string host, string database, string user, string password, string port = "3306")
        {
            bool isConnected = false;
            while (!isConnected)
            {
                try
                {
                    // Verificar la conexión a Internet aquí

                    connection = new MySqlConnection($"Server={host};Port={port};Database={database};Uid={user};Password={password}");
                    connection.Open();

                    isConnected = true; // Si la conexión se establece correctamente, establecemos isConnected en true para salir del bucle y continuar con las operaciones en la base de datos
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al establecer la conexión a la base de datos: " + ex.Message);
                    Console.WriteLine("Reintentando la conexión en 5 segundos...");
                    Thread.Sleep(5000); // Esperar 5 segundos antes de volver a intentar la conexión
                }
            }

            // Realizar las operaciones en la base de datos
        }


        //----------------------------------------
        public async Task<bool> UploadReadingMultiAxial(Phantom.MessageDataEIMONanoAccel data, string nombre_archivo, int id_punto)
        {
            MySqlCommand cmd = connection.CreateCommand();

            cmd.CommandText = "INSERT INTO lectura VALUES (@sensorid, @date, @id_punto, @rate, @temp, @ch1, @ch2, @ch3, 0, NULL, NULL);";

            cmd.Parameters.Add("@sensorid", MySqlDbType.Int64).Value = data.phantom_code; //forced to one
            cmd.Parameters.Add("@date", MySqlDbType.DateTime).Value = data.realnodetime;
            cmd.Parameters.Add("@id_punto", MySqlDbType.Int64).Value = id_punto;
            cmd.Parameters.Add("@rate", MySqlDbType.Int64).Value = data.sample_rate;
            cmd.Parameters.Add("@temp", MySqlDbType.Float).Value = data.temperature;
            cmd.Parameters.AddWithValue("@ch1", @"X - " + data.phantom_code + nombre_archivo + ".csv");
            cmd.Parameters.AddWithValue("@ch2", @"Y - " + data.phantom_code + nombre_archivo + ".csv");
            cmd.Parameters.AddWithValue("@ch3", @"Z - " + data.phantom_code + nombre_archivo + ".csv");

            try { await cmd.ExecuteNonQueryAsync(); cmd.Dispose(); return true; }
            catch (Exception fallo) { Console.WriteLine(fallo.ToString()); cmd.Dispose(); return false; }
        }

        public async Task<bool> UploadReadingMonoAxial(Phantom.MessageDataEIMONanoAccel data, string nombre_archivo, int id_punto, string eje)
        {
            MySqlCommand cmd = connection.CreateCommand();

            if (eje == "X")
            {
                cmd.CommandText = "INSERT INTO lectura VALUES (@sensorid, @date, @id_punto, @rate, @temp, @ch1, NULL, NULL, 0, NULL, NULL);";
            }

            if (eje == "Y")
            {
                cmd.CommandText = "INSERT INTO lectura VALUES (@sensorid, @date, @id_punto, @rate, @temp, NULL, @ch2, NULL, 0, NULL, NULL);";
            }

            if (eje == "Z")
            {
                cmd.CommandText = "INSERT INTO lectura VALUES (@sensorid, @date, @id_punto, @rate, @temp, NULL, NULL, @ch3, 0, NULL, NULL);";
            }


            cmd.Parameters.Add("@sensorid", MySqlDbType.Int64).Value = data.phantom_code; //forced to one
            cmd.Parameters.Add("@date", MySqlDbType.DateTime).Value = data.realnodetime;
            cmd.Parameters.Add("@id_punto", MySqlDbType.Int64).Value = id_punto;
            cmd.Parameters.Add("@rate", MySqlDbType.Int64).Value = data.sample_rate;
            cmd.Parameters.Add("@temp", MySqlDbType.Float).Value = data.temperature;
            // cmd.Parameters.AddWithValue("@ch1", @"X - " + data.phantom_code + nombre_archivo + ".csv");
            if (eje == "X")
            {
                cmd.Parameters.AddWithValue("@ch1", @"X - " + nombre_archivo + ".csv");
            }

            if (eje == "Y")
            {
                cmd.Parameters.AddWithValue("@ch2", @"Y - " + nombre_archivo + ".csv");
            }

            if (eje == "Z")
            {
                cmd.Parameters.AddWithValue("@ch3", @"Z - " + nombre_archivo + ".csv");
            }


            try { await cmd.ExecuteNonQueryAsync(); cmd.Dispose(); return true; }
            catch { cmd.Dispose(); return false; }

        }

        public int SelectSensor(string sensor_id)
        {
            MySqlCommand cmd = connection.CreateCommand();
            cmd.Connection = connection;

            cmd.CommandText = $"SELECT id_punto FROM punto WHERE codigo_sensor={sensor_id};";
            Int32 result = Convert.ToInt32(cmd.ExecuteScalar());

            return result;
        }
    }
}
