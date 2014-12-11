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

            if (addToTable("all", noun1, noun2))
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

        public bool insertInTable(string table, string noun1, string noun2)
        {
            if (!checkContradictions(noun1, noun2))
                return false;
            addToTable(table, noun1, noun2);

            if (!makeInferences(table, noun1, noun2))
            {
                removeFromTable(table, noun1, noun2);
                return false;
            }
            else
                return true;
        }

        private bool makeInferences(string table, string noun1, string noun2)
        {
            //No Inferences can be made when adding to the some table
            if (table == "some")
            {
                return true;
            }

            //Alright so here is the logic:
            //  We are adding noun1 == noun2
            //  If there exists a rule noun2 == other_noun
            //  Then we can infer that noun1 == other_noun
            m_dbConnection.Open();
            string sql = String.Format("select noun2 from rules_{0} where noun1 = \"{1}\"", table, noun2);
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            List<string> other_nouns = new List<string>();
            while (reader.Read())
            {
                string other_noun = (string)reader["noun2"];
                Console.WriteLine(string.Format("Infering that {0} {1} are {2}", table, noun1, other_noun));
                other_nouns.Add(other_noun);
            }
            m_dbConnection.Close();

            Console.WriteLine(other_nouns.Count);
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
        public void test_addToTable()
        {
            Console.WriteLine("\n\n   Testing addToTable()   \n__________________________");

            // We need a clean DB for consistant results
            reset();

            //Test: Inserting values to empty InfEng
            //Assert: True
            Console.WriteLine("Test 1: Adding Dog and Mammal");
            
            if (!addToTable("all", "Dog", "Mammal"))
                failure();
            else
                success();

            //Test: non-unique noun1
            //Assert: True
            Console.WriteLine("Test 2: Adding Dog and Wet");
            if (!addToTable("all", "Dog", "Wet"))
                failure();
            else
                success();

            //Test: non-unique noun2
            //Assert: True
            Console.WriteLine("Test 3: Adding Cat and Mammal");
            if (!addToTable("all", "Cat", "Mammal"))
                failure();
            else
                success();

            //Test: non-unique noun1 and noun2
            //Assert: False
            Console.WriteLine("Test 4: Adding Cat and Mammal");
            if (addToTable("all", "Cat", "Mammal"))
                failure();
            else
                success();

            return;
        }


        //TODO: Desparately need to clean this up with some sort of a query function
        //Tests are performed by quering the tables for values that we expect to not be there
        public void test_removeFromTable()
        {
            Console.WriteLine("\n\n   Testing removeFromTable()   \n_______________________________");

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
                success();
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
                success();
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
                success();
            else
                Console.WriteLine(string.Format("Test failed: {0} iterations\n", iterations));
            m_dbConnection.Close();
        }

        public void test_checkContradictions()
        {
            // We need a clean DB for consistant results
            reset();

            Console.WriteLine("\n\n   Testing checkContradictions()   \n___________________________________");

            //Test: No contradictions in an empty DB
            //Assert: True
            Console.WriteLine("Test 1: Contradictions in an empty DB?");
            if (!checkContradictions("dog", "mammals"))
                failure();
            else
                success();

            //Test: Contradiction in all table
            //Assert: False
            Console.WriteLine("Test 2: Contradiction in All table");
            addToTable("all", "dog", "mammals");
            if (checkContradictions("dog", "mammals"))
                failure();
            else
                success();
            removeFromTable("all", "dog", "mammals");

            //Test: Contradiction in no table
            //Assert: False
            Console.WriteLine("Test 3: Contradiction in No table");
            addToTable("no", "dog", "mammals");
            if (checkContradictions("dog", "mammals"))
                failure();
            else
                success();
            removeFromTable("no", "dog", "mammals");

            //Test: Contradiction in some table
            //Assert: False
            Console.WriteLine("Test 4: Contradiction in Some table");
            addToTable("some", "dog", "mammals");
            if (checkContradictions("dog", "mammals"))
                failure();
            else
                success();
            removeFromTable("some", "dog", "mammals");

            return;
        }

        public void test_makeInferences()
        {
            reset();

            //Test: Make inference on some table
            //Assert: True
            Console.WriteLine("Test 1: Inference on Some table");
            if (makeInferences("some", "dog", "mammal"))
                success();
            else
                failure();

            //Test: No inference made on non-existant values
            //Assert: True
            Console.WriteLine("Test 2: No inference made on non-existant values");
            if (makeInferences("all", "dog", "mammal"))
                success();
            else
                failure();

            //Test: No inference made on existant values
            //Assert: True
            Console.WriteLine("Test 3: No inference made on existant values");
            addToTable("all", "dog", "fuzzy");
            if (makeInferences("all", "dog", "mammal"))
                success();
            else
                failure();
            removeFromTable("all", "dog", "fuzzy");

            //Test: 1 Inference to be made
            Console.WriteLine("Test 4: 1 Inference to be made");
            addToTable("all", "dog", "mammal");
            addToTable("all", "mammal", "furry");
            if (makeInferences("all", "dog", "mammal"))
                success();
            else
                failure();
            removeFromTable("all", "dog", "mammal");
            removeFromTable("all", "mammal", "furry");


        }

        private void success()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("** Test Success **\n");
            Console.ForegroundColor = ConsoleColor.White;
        }

        private void failure()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Test failed\n");
            Console.ForegroundColor = ConsoleColor.White;
        }




    }
}
