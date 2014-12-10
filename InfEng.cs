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

        private bool addToTable(string table, string noun1, string noun2)
        {
            bool result = true;
            string sql = string.Format("INSERT INTO rules_{0}(noun1, noun2) VALUES (\"{1}\", \"{2}\")", table, noun1, noun2);
            
            m_dbConnection.Open();

            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            try
            {
                command.ExecuteNonQuery();
            }
            catch (SQLiteException exeption)
            {
                Console.WriteLine(exeption.Message);
                result = false;
            }

            m_dbConnection.Close();
            if (result)
                Console.WriteLine(string.Format("Added {0} and {1} to {2}", noun1, noun2, table));

            return result;
        }


        private bool removeFromTable(string table, string noun1, string noun2)
        {
            bool result = true;
            m_dbConnection.Open();
            string sql = string.Format("DELETE FROM rules_{0} where noun1 = \"{1}\" and noun2 =  \"{2}\"", table, noun1, noun2);
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            
            try
            {
                command.ExecuteNonQuery();
            }
            catch (SQLiteException exeption)
            {
                Console.WriteLine(exeption.Message);
                result = false;
            }

            m_dbConnection.Close();
            return result;
        }


        //Returns false if a contradiction is found in any table
        private bool checkContradictions(string noun1, string noun2)
        {
            //Attempt to add the values to all tables.  addToTable will return 
            //  false if the set can't be added due to a unique violation.
            //  In which case there is a contradiction and we return false.
            bool result;

            result = addToTable("all", noun1, noun2);
            if (result)
                removeFromTable("all", noun1, noun2);
            else
            {
                Console.WriteLine("Contradiction found in All");
                return false;
            }

            if (addToTable("no", noun1, noun2))
                removeFromTable("no", noun1, noun2);
            else
            {
                Console.WriteLine("Contradiction found in No");
                return false;
            }

            if (addToTable("some", noun1, noun2))
                removeFromTable("some", noun1, noun2);
            else
            {
                Console.WriteLine("Contradiction found in Some");
                return false;
            }


            return true;
        }

        public bool insAll(string noun1, string noun2)
        {
            if (!addToTable("all", noun1, noun2))
                return false;


            return true;
        }

        public void reset()
        {
            m_dbConnection.Open();
            string sql = "DELETE FROM rules_all";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();

            sql = "DELETE FROM rules_no";
            command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();

            sql = "DELETE FROM rules_some";
            command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();

            

            m_dbConnection.Close();
            return;
        }


        //WARNING: Calling any test_* functions will completely wipe the InfEng DB
        public void test_addAll()
        {
            // We need a clean DB for consistant results
            reset();

            //Test: Inserting values to empty InfEng
            //Assert: True
            Console.WriteLine("Test 1: Adding Dog and Mammal");
            
            if (!addToTable("all", "Dog", "Mammal"))
                Console.WriteLine("Test failed\n");
            else
                Console.WriteLine("** Test Success\n");

            //Test: non-unique noun1
            //Assert: True
            Console.WriteLine("Test 2: Adding Dog and Wet");
            if (!addToTable("all", "Dog", "Wet"))
                Console.WriteLine("Test failed\n");
            else
                Console.WriteLine("** Test Success\n");

            //Test: non-unique noun2
            //Assert: True
            Console.WriteLine("Test 3: Adding Cat and Mammal");
            if (!addToTable("all", "Cat", "Mammal"))
                Console.WriteLine("Test failed\n");
            else
                Console.WriteLine("** Test Success\n");

            //Test: non-unique noun1 and noun2
            //Assert: False
            Console.WriteLine("Test 4: Adding Cat and Mammal");
            if (addToTable("all", "Cat", "Mammal"))
                Console.WriteLine("Test failed\n");
            else
                Console.WriteLine("** Test Success\n");

            return;
        }


        //TODO: Desparately need to clean this up with some sort of a query function
        //Tests are performed by quering the tables for values that we expect to not be there
        public void test_removeAll()
        {
            // We need a clean DB for consistant results
            reset();

            //Test: Delete values from empty table
            //Assert: False
            Console.WriteLine("Test 1: Removing Dog and Mammal");
            removeFromTable("all", "Dog", "Mammal");
            m_dbConnection.Open();
            string sql = String.Format("select * from rules_all where noun1 = \"Dog\" and noun2 = \"Mammal\"");
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            int iterations = 0;
            while (reader.Read())
            {
                iterations++;
            }
            if (iterations == 0)
                Console.WriteLine("** Test Success\n");
            else
                Console.WriteLine(string.Format("Test failed: {0} iterations\n", iterations));
            m_dbConnection.Close();


            //Test: Delete existing values from table
            //Assert: True
            Console.WriteLine("Test 2: Removing Dog and Mammal");
            addToTable("all", "Dog", "Mammal");
            removeFromTable("all", "Dog", "Mammal");

            m_dbConnection.Open();
            sql = String.Format("select * from rules_all where noun1 = \"Dog\" and noun2 = \"Mammal\"");
            command = new SQLiteCommand(sql, m_dbConnection);
            reader = command.ExecuteReader();
            iterations = 0;
            while (reader.Read())
            {
                iterations++;
            }
            if (iterations == 0)
                Console.WriteLine("** Test Success\n");
            else
                Console.WriteLine(string.Format("Test failed: {0} iterations\n", iterations));
            m_dbConnection.Close();


            //Test: Delete non-existing values from non-empty table
            //Assert: False
            Console.WriteLine("Test 3: Removing Alligator and Mammal");
            addToTable("all", "Cat", "Mammal");
            addToTable("all", "Dog", "Mammal");
            removeFromTable("all", "Alligator", "Mammal");

            m_dbConnection.Open();
            sql = String.Format("select * from rules_all where noun1 = \"Alligator\" and noun2 = \"Mammal\"");
            command = new SQLiteCommand(sql, m_dbConnection);
            reader = command.ExecuteReader();
            iterations = 0;
            while (reader.Read())
            {
                iterations++;
            }
            if (iterations == 0)
                Console.WriteLine("** Test Success\n");
            else
                Console.WriteLine(string.Format("Test failed: {0} iterations\n", iterations));
            m_dbConnection.Close();
        }

        public void test_checkContradictions()
        {
            // We need a clean DB for consistant results
            reset();

            //Test: No contradictions in an empty DB
            //Assert: True
            Console.WriteLine("Test 1: Contradictions in an empty DB?");
            if (checkContradictions("dog", "mammals"))
                Console.WriteLine("Test failed\n");
            else
                Console.WriteLine("** Test Success\n");
            reset();

            //Test: Contradiction in all table
            //Assert: False
            Console.WriteLine("Test 2: Contradiction in All table");
            addToTable("all", "dog", "mammals");
            if (!checkContradictions("dog", "mammals"))
                Console.WriteLine("Test failed\n");
            else
                Console.WriteLine("** Test Success\n");
            reset();

            //Test: Contradiction in no table
            //Assert: False
            Console.WriteLine("Test 3: Contradiction in No table");
            addToTable("no", "dog", "mammals");
            if (!checkContradictions("dog", "mammals"))
                Console.WriteLine("Test failed\n");
            else
                Console.WriteLine("** Test Success\n");
            reset();

            //Test: Contradiction in some table
            //Assert: False
            Console.WriteLine("Test 4: Contradiction in Some table");
            addToTable("some", "dog", "mammals");
            if (!checkContradictions("dog", "mammals"))
                Console.WriteLine("Test failed\n");
            else
                Console.WriteLine("** Test Success\n");
            reset();

        }




    }
}
