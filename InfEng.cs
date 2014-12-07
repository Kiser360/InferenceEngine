using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;

namespace InferenceEngine
{
    class InfEng
    {
        private SQLiteConnection m_dbConnection;
        private string DB_filename;

        //CONSTRUCTOR: accepts an explicit filename for the DB, or uses a default value
        //RESULT:  The constructor will create the DB connection, then attempt to create 
        //  the DB tables if they do not already exist.
        public InfEng(string filename = "MyDatabase.sqlite")
        {
            DB_filename = filename;

            //Lets see if we need to create the DB
            if (!File.Exists(DB_filename))
            {
                SQLiteConnection.CreateFile(DB_filename);
            }

            //Create connection to the DB
            m_dbConnection = new SQLiteConnection("Data Source=" + DB_filename + ";Version=3;");
            m_dbConnection.Open();

            //Time to create the tables if necessary
            string create_ALL = "CREATE TABLE IF NOT EXISTS rules_all(noun1 TEXT NOT NULL, noun2 TEXT NOT NULL, UNIQUE(noun1, noun2))";
            string create_NO = "CREATE TABLE IF NOT EXISTS rules_no(noun1 TEXT NOT NULL, noun2 TEXT NOT NULL, UNIQUE(noun1, noun2))";
            string create_SOME = "CREATE TABLE IF NOT EXISTS rules_some(noun1 TEXT NOT NULL, noun2 TEXT NOT NULL, UNIQUE(noun1, noun2))";

            //Execute the SQL commands
            SQLiteCommand command = new SQLiteCommand(create_ALL, m_dbConnection);
            command.ExecuteNonQuery();
            command = new SQLiteCommand(create_NO, m_dbConnection);
            command.ExecuteNonQuery();
            command = new SQLiteCommand(create_SOME, m_dbConnection);
            command.ExecuteNonQuery();

            //All done with the DB for now
            m_dbConnection.Close();
        }

        private bool addAll(string noun1, string noun2)
        {
            string sql = "INSERT INTO all (to, from) VALUES (" + noun1 + ", " + noun2 + ")";
            
            m_dbConnection.Open();

            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            if (command.ExecuteNonQuery() < 1)
            {
                // Return false when no new rows are added because of the 'unique' constraint violation
                return false;
            }
            else
                // Return true when successfully added entry to the table
                return true;
        }




    }
}
