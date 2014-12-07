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
            InfEng myEngine = new InfEng();
            myEngine = new InfEng("myCrazyPath.sqlite");



            //I NEED TIME TO READ THE CONSOLE!!!!
            //    **This pauses execution**
            Console.WriteLine("\n\nWaiting for you to hit enter to exit...");
            Console.Read();
        }
    }
}
