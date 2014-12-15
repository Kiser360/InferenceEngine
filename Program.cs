using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InferenceEngine
{
    class Program
    {
        static void Main(string[] args)
        {
            /*Un Comment this to run unit Tests
            //  -- Check console for results
            myEngine = new InfEng("newCrazyPath.sqlite");
            myEngine.test_addToTable();
            myEngine.test_removeFromTable();
            myEngine.test_checkContradictions();
            //HOLY FMG BATMAN!!!: THIS TEST TAKES LIKE 20 MINUTES!
            myEngine.test_insertIntoTable();
             * */

            bool endRun = false;
            string input;

            Console.WriteLine("          Inference Engine: Read-Eval-Print-Loop");
            Console.WriteLine("__________________________________________________________");
            Console.Write("\nEnter the path to DataBase: ");
            string dbPath = Console.ReadLine();
            while (dbPath.Length < 1)
            {
                Console.Write("\nInvalid Path, try again: ");
                dbPath = Console.ReadLine();
            }


            InfEng REPL_Engine = new InfEng(dbPath);
            
            Console.WriteLine("\n\nPlease input your commands at the prompt. Press enter with no command for help.");
            while(!endRun)
            {
                Console.Write("\n>>");
                input = Console.ReadLine().ToUpper();  //Make everything uppercase for consistancy

                //User wants to exit the program
                if (input == "EXIT")
                {
                    //So the loop will exit and end the program
                    endRun = true;
                }
                //User wants to test the program
                else if (input == "TEST")
                {
                    Console.WriteLine("HEY!! Lets test some stuffs...  Hope nothing breaks");
                }
                //User wants to reset the DB
                else if (input == "RESET")
                {
                    REPL_Engine.reset();
                    Console.WriteLine("Database is now empty");
                }
                //User wants to query the DB
                else if (input.Length > 0)
                {
                    if (input[0] == '?')
                    {
                        //Send the query to the DB
                        List<string> Q_Result = REPL_Engine.query(input.Substring(1));

                        //Format and output all results
                        for (int i = 0; i < Q_Result.Count; i++)
                        {
                            string[] splits = Q_Result[i].Split(':');
                            Console.WriteLine("-- " + String.Format("{0} {1} are {2}", splits[0].ToUpper(), splits[1], splits[2]));
                        }
                    }
                    //User wants to add to the DB
                    else if (input.Contains("ARE"))
                    {
                        if (!REPL_Engine.parse(input))
                            Console.WriteLine("Error with parsing");
                    }
                    else
                        Console.WriteLine("Try using this format: [ all | no | some ] <noun1> are <noun2>");
                }
                //User needs HALP!!
                else
                {
                    Console.Clear();
                    Console.WriteLine("\n\n     Here is everything you can do (not case sensitive):     " +
                                        "\n_____________________________________________________________" +

                                      "\n\n  EXIT         -  Will exit the program" +
                                        "\n  TEST         -  Will run tests to prove functionality" +
                                        "\n  RESET        -  Will reset the Database" +

                                      "\n\nAdding to the DB" +
                                      "\n\n  [ all | some | no ] <noun1> are <noun2>" +
                                        "\n               -  Will add the assertion to the Database" +

                                      "\n\nQuery the DB" +
                                      "\n\n  ?<noun>      -  Will display all information about the noun" +
                                        "\n  ?            -  Will display all information about all nouns");
                }
            }
        }
    }
}
