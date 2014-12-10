using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace InferenceEngine
{
    class Program
    {
        static void Main(string[] args)
        {
            //Briefly check the Constructor
            InfEng myEngine = new InfEng();
            myEngine = new InfEng("newCrazyPath.sqlite");

            //Test the private addAll() function
            //  -- Check console for results
            myEngine.test_addAll();

            myEngine.test_removeAll();


            //I NEED TIME TO READ THE CONSOLE!!!!
            //    **This pauses execution**
            Console.WriteLine("\n\nWaiting for you to hit enter to exit...");
            Console.Read();
        }
    }
}
