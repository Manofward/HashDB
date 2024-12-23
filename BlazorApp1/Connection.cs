﻿using DuckDB.NET.Data;
using Hashing.src;
using Hashing.src.interfaces;
using System.Data.Entity.Core.Mapping;

namespace ConnectDB
{
    public class Connection
    {
        // creating objects for later use
        private readonly ICust _cust;
        public DuckDBConnection _connection;

        // giving the objects from above values
        public Connection()
        {
            _cust = new Cust();
            _connection = DB_Connection();
        }

        // This function opens the connection to the db
        public DuckDBConnection DB_Connection()
        {
            var Create_Connection = new DuckDBConnection("Data Source=C:\\Users\\MeyerJan.FIAE\\Documents\\Aufgaben\\C#_Übungen\\BlazorApp1\\BlazorApp1\\TestDB.sql");
            Create_Connection.Open();

            // this code is only if the sql doesnt exist anymor
            /*
            using var command = Create_Connection.CreateCommand();

            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Accounts (
                User_ID INTEGER PRIMARY KEY,
                Username VARCHAR NOT NULL, 
                HashedPassword VARCHAR NOT NULL, 
                Salt VARCHAR NOT NULL
            );";

            command.ExecuteNonQuery();
            */
            return Create_Connection;
        }

        // function to close connection after closing
        public void Close_Connection()
        {
            if (_connection != null && _connection.State == System.Data.ConnectionState.Open)
            {
                _connection.Close();
            }
        }

        // this function saves the user to the db
        public bool Save_User_To_DB(string username, string password)
        {
            try
            {
                using var command = _connection.CreateCommand();

                // this is for incrementing the user_Id
                command.CommandText = "SELECT COALESCE(MAX(User_Id), 0) + 1 FROM Accounts";
                int newUserId = Convert.ToInt32(command.ExecuteScalar());

                // this is for creating the hash for the save later
                string salt = _cust.Salt(32);
                string hashed_Password = _cust.Hash("hello", salt, 32);

                // this is for inserting the salt and hashed password to the db
                command.CommandText = "INSERT INTO Accounts (User_Id, Username, HashedPassword, Salt) VALUES ($user_Id, $username, $hashed_Password, $salt);";
                command.Parameters.Clear();
                command.Parameters.Add(new DuckDBParameter("user_Id", newUserId));
                command.Parameters.Add(new DuckDBParameter("username", username));
                command.Parameters.Add(new DuckDBParameter("hashed_Password", hashed_Password));
                command.Parameters.Add(new DuckDBParameter("salt", salt));

                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Couldn't create Account:" + ex.Message);
                return false;
            }

            return true;
        }

        // this is the function to read the values from the db and give it out
        public List<string> Read_Values(int? user_Id = null, string? username = null)
        {
            var result = new List<string>();
            try
            {
                using var command = _connection.CreateCommand();

                // this is a query used to read the values
                command.CommandText = @"
                    SELECT 
                        *
                    FROM 
                        Accounts 
                    WHERE 
                        ($user_Id IS NULL OR User_ID = $user_Id) AND
                        ($username IS NULL OR Username = $username);";
                command.Parameters.Clear();
                command.Parameters.Add(new DuckDBParameter("user_Id", (object)user_Id ?? DBNull.Value));
                command.Parameters.Add(new DuckDBParameter("username", (object)username ?? DBNull.Value));

                // this is for the reading process and saving the result to a variable 
                using var reader = command.ExecuteReader();

                Console.WriteLine("Users in the database:");

                while (reader.Read())
                {
                    var line = new List<string>();

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        line.Add($"{reader.GetName(i)}:     {reader.GetValue(i)}");
                    }

                    result.Add(string.Join(", ", line));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Processing Failed: {ex.Message}");
                return result;
            }

            return result;
        }
    }
}
