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

            Console.WriteLine("          Inference Engine: Read-Eval-Print-Loop");
            Console.WriteLine("__________________________________________________________");
            Console.WriteLine("Please input your commands at the prompt. Press enter with no command for help.");


            bool endRun = false;
            string input;
            InfEng REPL_Engine = new InfEng("REPL_Engine.db");
            while(!endRun)
            {
                Console.Write(">>");
                input = Console.ReadLine().ToUpper();
                if (input == "EXIT")
                {
                    endRun = true;
                }
                else if (input[0] == '?')
                {
                    Console.WriteLine("Hey Thats a QUERY!");
                    List<string> Q_Result = REPL_Engine.query(input.Substring(1));
                    for (int i = 0; i < Q_Result.Count; i++)
                    {
                        string[] splits = Q_Result[i].Split(':');
                        Console.WriteLine("-- " + String.Format("{0} {1} are {2}", splits[0], splits[1], splits[2]));
                    }
                }
                else
                {
                    Console.WriteLine("Gonna need to parse this badboy");
                }
            }

        }
    }
}
