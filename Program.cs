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
            /*
            //Un Comment this to run unit Tests
            //  -- Check console for results
            //NOTE! I reused the success and fail messages to display insertion results,
            //     Therefore just because you see a fail message doesnt mean the test failed!
            //     You have to look at the last success/fail message before beginning the next test,
            //     That message is the true result of the test
            InfEng myEngine = new InfEng("UnitTest.sqlite");
            myEngine.test_addToTable();
            myEngine.test_removeFromTable();
            myEngine.test_checkContradictions();
            //HOLY FMG BATMAN!!!: THIS TEST TAKES LIKE 20 MINUTES!
            myEngine.test_insertIntoTable();
            */
            

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

            // Begin the loop, Read-Evaluate-Print
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
                    Console.WriteLine("HEY!! Lets test some stuffs...  Hope nothing breaks\n\n");
                    testing(REPL_Engine);
                }

                //User wants to reset the DB
                else if (input == "RESET")
                {
                    REPL_Engine.reset();
                    
                }

                //User wants to interact with DB
                else if (input.Length > 0)
                {
                    //User wants to query the DB
                    if (input[0] == '?')
                    {
                        //Send the query to the DB
                        outQueryResults(REPL_Engine.query(input.Substring(1)));

                        
                    }

                    //User wants to add to the DB
                    else if (input.Contains("ARE"))
                    {
                        //DB does the hardwork for us
                        if (!REPL_Engine.parse(input))
                            Console.WriteLine("Error with parsing");
                    }
                    //User messed up, lets help a bro out
                    else
                        Console.WriteLine("Try using this format: [ all | no | some ] <noun1> are <noun2>");
                }
               
                //User needs serious HALP!!
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

        public static void testing(InfEng engine)
        {
            Console.Clear();
            Console.WriteLine("EXIT  - Gonna have to let you test this one for yourself" +
                            "\nTEST  - Well I'm running aren't I?" +
                            "\nRESET - Message that the DB has been cleared and query should return nothing\n");
            Console.WriteLine("___________________________________________________");
            engine.reset();
            engine.query("");
            Console.Write("\nPress Enter to continue...");
            Console.ReadLine();

            //Insert an assertion
            Console.Clear();
            Console.WriteLine("\nInserting an Assertion - Expect Success");
            Console.WriteLine("Simple Assertion: ALL DOGS ARE MAMMALS");
            Console.WriteLine("___________________________________________________");
            engine.parse("ALL DOGS ARE MAMMALS");
            outQueryResults(engine.query(""));
            Console.Write("\nPress Enter to continue...");
            Console.ReadLine();

            //Insert Contradicting assertion
            Console.Clear();
            Console.WriteLine("\nInsert a Contradiction - Expect Fail");
            Console.WriteLine("Create contradiction: NO DOGS ARE MAMMALS");
            Console.WriteLine("Verify with a query on DOGS and expect not to see NO DOGS ARE MAMMALS");
            Console.WriteLine("___________________________________________________");
            engine.parse("NO DOGS ARE MAMMALS");
            outQueryResults(engine.query(""));
            Console.Write("\nPress Enter to continue...");
            Console.ReadLine();

            //Insert loop assertion
            Console.Clear();
            Console.WriteLine("\nInsertion loop-creating Assertion - Expect Fail");
            Console.WriteLine("This will test infinite loop creation by attempting to asert:");
            Console.WriteLine("ALL MAMMALS ARE DOGS");
            Console.WriteLine("___________________________________________________");
            engine.parse("ALL MAMMALS ARE DOGS");
            outQueryResults(engine.query(""));
            engine.reset();
            Console.Write("\nPress Enter to continue...");
            Console.ReadLine();

            //Barbara X==Y Y==Z :: X==Z Inference
            Console.Clear();
            Console.WriteLine("\nBarbara: X==Y Z==X :: Z==Y - Expect Success");
            Console.WriteLine("     - all mammals are have_fur");
            Console.WriteLine("     - all dogs are mammals");
            Console.WriteLine("Infer: all dogs are have_fur");
            Console.WriteLine("___________________________________________________");
            engine.parse("ALL MAMMALS ARE HAVE_FUR");
            engine.parse("ALL DOGS ARE MAMMALS");
            outQueryResults(engine.query(""));
            engine.reset();
            Console.Write("\nPress Enter to continue...");
            Console.ReadLine();

            //Barbara Recursive Inference
            Console.Clear();
            Console.WriteLine("\nBarbara Recursive Inference");
            Console.WriteLine("               - all dogs are mammals");
            Console.WriteLine("               - all mammals are have_fur");
            Console.WriteLine("               - all pugs are dogs");
            Console.WriteLine("Level 1   Infer: all pugs are mammals");
            Console.WriteLine("Recursive Infer: all pugs are have_fur");
            Console.WriteLine("___________________________________________________");
            engine.parse("ALL DOGS ARE MAMMALS");
            engine.parse("ALL MAMMALS ARE HAVE_FUR");
            engine.parse("ALL PUGS ARE DOGS");
            outQueryResults(engine.query(""));
            engine.reset();
            Console.Write("\nPress Enter to continue...");
            Console.ReadLine();

            //Recursive into error and revert changes
            Console.Clear();
            Console.WriteLine("\nRecurse into error");
            Console.WriteLine("\nExpect failure to insert and revert inferences");
            Console.WriteLine("               - no pugs are have_fur");
            Console.WriteLine("               - all dogs are mammals");
            Console.WriteLine("               - all mammals are have_fur");
            Console.WriteLine("               - all pugs are dogs");
            Console.WriteLine("Level 1   Infer: all pugs are mammals");
            Console.WriteLine("Recursive Infer: all pugs are have_fur");
            Console.WriteLine("          Error: Contradiction revert inferences");
            Console.WriteLine("___________________________________________________");
            engine.parse("NO PUGS ARE HAVE_FUR");
            engine.parse("ALL DOGS ARE MAMMALS");
            engine.parse("ALL MAMMALS ARE HAVE_FUR");
            engine.parse("ALL PUGS ARE DOGS");
            outQueryResults(engine.query(""));
            engine.reset();
            Console.Write("\nPress Enter to continue...");
            Console.ReadLine();


            //Calarent Syllogism

            //Calarent Syllogism from an inference (recursive)

            //Datisi Syllogism

            //Recursive Datisi + Barbara




            return;
        }

        public static void outQueryResults(List<string> results)
        {
            //Format and output all results
            for (int i = 0; i < results.Count; i++)
            {
                string[] splits = results[i].Split(':');
                Console.WriteLine("-- " + String.Format("{0} {1} are {2}", splits[0].ToUpper(), splits[1], splits[2]));
            }
        }
    }
}
