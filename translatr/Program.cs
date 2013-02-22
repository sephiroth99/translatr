using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace translatr
{
    class Program
    {
        private static void showHelpAndQuit()
        {
            Console.WriteLine("Usage: translatr MODE [OPTIONS...]");
            Console.WriteLine("");
            Console.WriteLine("MODE:");
            Console.WriteLine("  extract    Extract all translatable text from game files");
            Console.WriteLine("  apply      Apply translation to game files");
            Console.WriteLine("");
            Console.WriteLine("For more details on each command, run \"translatr MODE help\"");
            System.Environment.Exit(0);
        }

        static void Main(string[] args)
        {
            Console.WriteLine("");
            Console.WriteLine("translatr 0.2.1");
            Console.WriteLine("by sephiroth99");
            Console.WriteLine("");

            if (args.Length > 0)
            {
                if (args[0] == "apply")
                {
                    Applicator.doApply(args);
                }
                else if (args[0] == "extract")
                {
                    Extractor.doExtract(args);
                }
                else
                {
                    showHelpAndQuit();
                }
            }
            else
            {
                showHelpAndQuit();
            }                
        }
    }
}
