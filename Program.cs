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
            //Briefly check the Constructor
            InfEng myEngine = new InfEng();
            myEngine = new InfEng("newCrazyPath.sqlite");

            //Unit Tests for the private functions
            //  -- Check console for results
            myEngine.test_addToTable();

            myEngine.test_removeFromTable();

            myEngine.test_checkContradictions();

            myEngine.test_makeInferences();


            //I NEED TIME TO READ THE CONSOLE!!!!
            //    **This pauses execution**
            Console.WriteLine("\n\nWaiting for you to hit enter to exit...");
            Console.Read();
        }
    }
}
