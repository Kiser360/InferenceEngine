﻿using System;
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
        private List<string> undo_chain;
        private bool isUndoOpen;

        //CONSTRUCTOR: accepts an explicit filename for the DB, or uses a default value
        //RESULT:  The constructor will create the DB connection, then attempt to create 
        //  the DB tables if they do not already exist.
        public InfEng(string filename = "MyDatabase.sqlite")
        {
            DB_filename = filename;
            undo_chain = new List<string>();
            isUndoOpen = false;

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
                //Console.WriteLine(exeption.Message);
                result = false;
            }

            m_dbConnection.Close();
            if (!result)
                Console.WriteLine(string.Format("Can't Add {0} and {1} to {2}", noun1, noun2, table));

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


        //Returns 0 if a contradiction is found in table other than targetTable
        //Returns 1 if a contradiction found in targetTable
        //Returns 2 if no contradiction is found
        private int checkContradictions(string targetTable, string noun1, string noun2)
        {
            //Attempt to add the values to all tables.  addToTable will return 
            //  false if the set can't be added due to a unique violation.
            //  In which case there is a contradiction, then we need to check if
            //  we are trying to add to the target table in which case its not really
            //  a contradiction, just already known information
            bool addToAllTable = addToTable("ALL", noun1, noun2);
            if (addToAllTable)
                removeFromTable("ALL", noun1, noun2);
            else if (!addToAllTable && targetTable == "ALL")
                return 1;
            else if (!addToAllTable)
                return 0;
            
            bool addToNoTable = addToTable("NO", noun1, noun2);
            if (addToNoTable)
                removeFromTable("NO", noun1, noun2);
            else if (!addToNoTable && targetTable == "NO")
                return 1;
            else if (!addToNoTable)
                return 0;

            bool addToSomeTable = addToTable("SOME", noun1, noun2);
            if (addToSomeTable)
                removeFromTable("SOME", noun1, noun2);
            else if (!addToSomeTable && targetTable == "SOME")
                return 1;
            else if (!addToSomeTable)
                return 0;


            return 2;
        }

        public bool insertInTable(string table, string noun1, string noun2)
        {
            //This is used only on the first call of insertInTable, these variables insure that
            //  recursively called insertInTable's don't damage the undoChain which is necessary
            //  when a contradiction is found.
            bool needToCloseUndo = false;
            if (!isUndoOpen)
            {
                isUndoOpen = true;
                needToCloseUndo = true;
                undo_chain.Add(table);  //The first element of the chain will always be the targetTable
            }

            int contradictionType = checkContradictions(table, noun1, noun2);

            
            if (contradictionType == 0)       //True contradiction with another table
            {
                Console.WriteLine("Contradiction with another table");
                if (needToCloseUndo)
                {
                    undo_chain.Clear();
                    isUndoOpen = false;
                    failure();
                }
                return false;
            }
            else if (contradictionType == 1)  //Already known knowledge
            {
                Console.WriteLine("Already Known Information");
            }
            else if (contradictionType == 2)  //No contradictions found
            {
                addToTable(table, noun1, noun2);
                undo_chain.Add(noun1);
                undo_chain.Add(noun2);
            }
            

            if (!makeInferences(table, noun1, noun2))
            {
                if(needToCloseUndo)      //if there is a contradiction anywhere recursively and this is the first instance of insertIntoTable
                {
                    revert();            //Run through the undoChain and revert all the changes made
                    undo_chain.Clear();
                    isUndoOpen = false;
                }
                failure();
                return false;
            }
            else
            {
                if (needToCloseUndo)
                {
                    undo_chain.Clear();
                    isUndoOpen = false;
                    success();
                }
                return true;
            }
                
        }

        private bool makeInferences(string table, string noun1, string noun2)
        {
            //Equvalence Case: X==Y Y==X
            //  If we don't account for this the other checks will result in permanent loop
            m_dbConnection.Open();
            string sql = String.Format("select noun2 from rules_{0} where noun1 = \"{1}\" and noun2 = \"{2}\"", table, noun2, noun1);
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            List<string> other_nouns = new List<string>();
            if (reader.Read())
            {
                m_dbConnection.Close();
                removeFromTable(table, noun1, noun2);
                Console.WriteLine("Error: Already known, {0} {1} are {2}",table, noun2, noun1);
                return false;
            }
            m_dbConnection.Close();

            //Barbara Syllogism: Y==Z X==Y :: X==Z
            //  We are adding noun1 == noun2
            //  If there exists a rule noun2 == other_noun
            //  Then we can infer that noun1 == other_noun
            if (table == "ALL")
            {
                m_dbConnection.Open();
                sql = String.Format("select noun2 from rules_{0} where noun1 = \"{1}\"", table, noun2);
                command = new SQLiteCommand(sql, m_dbConnection);
                reader = command.ExecuteReader();
                other_nouns = new List<string>();
                while (reader.Read())
                {
                    string other_noun = (string)reader["noun2"];
                    Console.WriteLine(string.Format("Infering that {0} {1} are {2}", table, noun1, other_noun));
                    other_nouns.Add(other_noun);
                }
                m_dbConnection.Close();

                for (int i = 0; i < other_nouns.Count; i++)
                {
                    if (!insertInTable(table, noun1, other_nouns[i]))
                    {
                        return false;
                    }
                }
            }

            //Calarent Syllogism: X!=Y Z==X :: Z!=Y
            //  Adding noun1 == noun2
            //  if there exists a rule noun2 != other_noun
            //  Then we can infer that noun1 != other_noun
            if (table == "ALL")
            {
                m_dbConnection.Open();
                sql = String.Format("select noun2 from rules_{0} where noun1 = \"{1}\"", "NO", noun2);
                command = new SQLiteCommand(sql, m_dbConnection);
                reader = command.ExecuteReader();
                other_nouns = new List<string>();
                while (reader.Read())
                {
                    string other_noun = (string)reader["noun2"];
                    Console.WriteLine(string.Format("Infering that {0} {1} are {2}", "NO", noun1, other_noun));
                    other_nouns.Add(other_noun);
                }
                m_dbConnection.Close();

                for (int i = 0; i < other_nouns.Count; i++)
                {
                    if (!insertInTable("NO", noun1, other_nouns[i]))
                    {
                        return false;
                    }
                }
            }


            //Datisi Syllogism: X==Y Z~=X :: Z~=Y
            //  Adding noun1 ~= noun2
            //  if there exists a rule noun2 == other_noun
            //  Then we can infer that noun1 ~= other_noun
            if (table == "SOME")
            {
                m_dbConnection.Open();
                sql = String.Format("select noun2 from rules_{0} where noun1 = \"{1}\"", "ALL", noun2);
                command = new SQLiteCommand(sql, m_dbConnection);
                reader = command.ExecuteReader();
                other_nouns = new List<string>();
                while (reader.Read())
                {
                    string other_noun = (string)reader["noun2"];
                    Console.WriteLine(string.Format("Infering that {0} {1} are {2}", "SOME", noun1, other_noun));
                    other_nouns.Add(other_noun);
                }
                m_dbConnection.Close();

                for (int i = 0; i < other_nouns.Count; i++)
                {
                    if (!insertInTable("SOME", noun1, other_nouns[i]))
                    {
                        return false;
                    }
                }
            }

            //There are many other Syllogisms but I haven't implemented a SOME_NOT 
            //  table so were not going to be able to do it.. :(

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
            Console.WriteLine("Database is now empty");
            return;
        }

        //Returns all known information about a given noun
        //Results returned in the form of a string with : delimeters
        //  - First entry is the table, second is noun1, third is noun2
        public List<string> query(string noun)
        {
            List<string> results = new List<string>();

            m_dbConnection.Open();

            //Whats in the All table?????
            string sql = string.Format("SELECT * FROM rules_all WHERE noun1 = \"{0}\"", noun);
              //Get all info from table if noun is empty
            if (noun.Length < 1)
                sql = string.Format("SELECT * FROM rules_all ORDER BY noun1");

            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                results.Add(String.Format("{0}:{1}:{2}","ALL", reader["noun1"], reader["noun2"]));
            }

            //Whats in the No table?????
            sql = string.Format("SELECT * FROM rules_no WHERE noun1 = \"{0}\"", noun);
              //All info again
            if (noun.Length < 1)
                sql = string.Format("SELECT * FROM rules_no ORDER BY noun1");

            command = new SQLiteCommand(sql, m_dbConnection);
            reader = command.ExecuteReader();
            while (reader.Read())
            {
                results.Add(String.Format("{0}:{1}:{2}", "NO", reader["noun1"], reader["noun2"]));
            }

            //Whats in the Some table??????
            sql = string.Format("SELECT * FROM rules_some WHERE noun1 = \"{0}\"", noun);
              //all info again
            if (noun.Length < 1)
                sql = string.Format("SELECT * FROM rules_some ORDER BY noun1");

            command = new SQLiteCommand(sql, m_dbConnection);
            reader = command.ExecuteReader();
            while (reader.Read())
            {
                results.Add(String.Format("{0}:{1}:{2}", "SOME", reader["noun1"], reader["noun2"]));
            }

            m_dbConnection.Close();

            return results;
        }

        public bool parse(string input)
        {
            string[] nouns = input.ToUpper().Split(' ');
            if (nouns.Length != 4)
            {
                Console.WriteLine("Syntax Error: Too Many Words");
                return false;
            }
            if (nouns[0] != "ALL" && nouns[0] != "NO" && nouns[0] != "SOME")
            {
                Console.WriteLine("Syntax Error: Doesn't contain Keyword");
                return false;
            }
            else
                insertInTable(nouns[0], nouns[1], nouns[3]);

            return true;

        }

        private void revert()
        {
            for (int i = 1; i < undo_chain.Count; i += 2 )
            {
                removeFromTable(undo_chain[0], undo_chain[i], undo_chain[i + 1]);
            }

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
            
            if (!addToTable("ALL", "DOG", "Mammal"))
                failure();
            else
                success();

            //Test: non-unique noun1
            //Assert: True
            Console.WriteLine("Test 2: Adding Dog and Wet");
            if (!addToTable("ALL", "DOG", "Wet"))
                failure();
            else
                success();

            //Test: non-unique noun2
            //Assert: True
            Console.WriteLine("Test 3: Adding Cat and Mammal");
            if (!addToTable("ALL", "Cat", "Mammal"))
                failure();
            else
                success();

            //Test: non-unique noun1 and noun2
            //Assert: False
            Console.WriteLine("Test 4: Adding Cat and Mammal");
            if (addToTable("ALL", "Cat", "Mammal"))
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
            removeFromTable("ALL", "DOG", "Mammal");
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
            addToTable("ALL", "DOG", "Mammal");
            removeFromTable("ALL", "DOG", "Mammal");

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
            addToTable("ALL", "Cat", "Mammal");
            addToTable("ALL", "DOG", "Mammal");
            removeFromTable("ALL", "Alligator", "Mammal");

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
            //Returns 0 if a contradiction is found in table other than targetTable
            //Returns 1 if a contradiction found in targetTable
            //Returns 2 if no contradiction is found

            // We need a clean DB for consistant results
            reset();

            Console.WriteLine("\n\n   Testing checkContradictions()   " +
                                "\n___________________________________");

            //Test: No contradictions in an empty DB
            //Assert: True
            Console.WriteLine("Test 1: Contradictions in an empty DB?");
            if (checkContradictions("ALL", "DOG", "mammals") != 2)
                failure();
            else
                success();

            //Test: Contradiction in all table
            //Assert: False
            Console.WriteLine("Test 2: Contradiction in All table");
            addToTable("ALL", "DOG", "mammals");
            if (checkContradictions("NO","DOG", "mammals") != 0)
                failure();
            else
                success();
            removeFromTable("ALL", "DOG", "mammals");

            //Test: Contradiction in no table
            //Assert: False
            Console.WriteLine("Test 3: Contradiction in No table");
            addToTable("NO", "DOG", "mammals");
            if (checkContradictions("ALL", "DOG", "mammals") != 0)
                failure();
            else
                success();
            removeFromTable("NO", "DOG", "mammals");

            //Test: Contradiction in some table
            //Assert: False
            Console.WriteLine("Test 4: Contradiction in Some table");
            addToTable("SOME", "DOG", "mammals");
            if (checkContradictions("ALL", "DOG", "mammals") != 0)
                failure();
            else
                success();
            removeFromTable("SOME", "DOG", "mammals");

            return;
        }

        public void test_insertIntoTable()
        {
            reset();

            //Test: insert to empty table
            //Assert: true
            Console.WriteLine("Test 1: Insert into empty DB");
            if (insertInTable("ALL", "DOG", "MAMMAL"))
                success();
            else
                failure();

            //Test: insert contradictory info
            //Assert: false
            Console.WriteLine("Test 2: Contradictory information");
            if (!insertInTable("NO", "DOG", "MAMMAL"))
                success();
            else
                failure();


            //Test: insert and make recursive X==Y Z==X Z==Y inference
            //Assert: False
            Console.WriteLine("Test 3: recursive inference Z==Y");
            insertInTable("ALL", "cat", "mammal");
            insertInTable("ALL", "furry", "has_hair");
            if (!insertInTable("NO", "DOG", "has_hair"))
                success();
            else
                failure();

            //Test: revert changes from a shallow recursive inference Error
            //Assert: False
            Console.WriteLine("Test 4: shallow recursive error");
            reset();
            insertInTable("ALL", "thing1", "thing2");
            insertInTable("ALL", "thing2", "thing3");
            insertInTable("ALL", "thing3", "thing4");
            insertInTable("SOME", "thing4", "thing5");
            if (!insertInTable("ALL", "thing4", "thing5"))
                success();
            else
                failure();

            //Test: revert changes from deep recursive X==Z inference Error
            //Assert: True
            Console.WriteLine("Test 5: deep recursive error X==Z");
            reset();
            insertInTable("NO", "thing10", "thing11");
            insertInTable("NO", "thing20", "thing21");
            insertInTable("NO", "thing29", "thing30");
            for (int i = 0; i < 30; i++)
            {
                insertInTable("ALL", "thing" + i, "thing" + (i + 1));
            }
            if (insertInTable("NO", "thing1", "thing11"))  //I should be able to add this because this inference was never made in the all table
                success();
            else
                failure();

            //Test: revert changes from deep recursive Z==Y inference Error
            //Assert: True
            Console.WriteLine("Test 6: deep recursive error Z==Y");
            reset();
            insertInTable("NO", "thing10", "thing11");
            insertInTable("NO", "thing20", "thing21");
            insertInTable("NO", "thing29", "thing30");
            for (int i = 0; i < 30; i++)
            {
                insertInTable("ALL", "thing" + (i + 1), "thing" + (i - 1));
            }
            if (insertInTable("NO", "thing1", "thing11"))  //I should be able to add this because this inference was never made in the all table
                success();
            else
                failure();


        }

        private void success()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("** Success **\n");
            Console.ForegroundColor = ConsoleColor.White;
        }

        private void failure()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("** Failure **\n");
            Console.ForegroundColor = ConsoleColor.White;
        }




    }
}
