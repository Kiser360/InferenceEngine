using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;

namespace InferenceEngine
{
    class Program
    {
        static void Main(string[] args)
        {
            const string DEF_DB = "MyDatabase.sqlite";

            //Lets see if we need to create the DB
            if (!File.Exists(DEF_DB))
            {
                SQLiteConnection.CreateFile(DEF_DB);
                Console.WriteLine("Creating DB for the first time");
            }



            //I NEED TIME TO READ THE CONSOLE!!!!
            //    **This pauses execution**
            Console.Read();
        }
    }
}
