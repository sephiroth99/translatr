using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace translatr
{
    class Program
    {
        static void Main(string[] args)
        {
            bool   extractMode  = true;
            bool   isBigEndian  = false;
            int    lang         = 1;
            String bigfilePath  = String.Empty;
            String patchPath    = String.Empty;
            String transPath    = String.Empty;
            String ovrBasePath  = String.Empty;
            String ovrPatchPath = String.Empty;

            Console.WriteLine("");
            Console.WriteLine("translatr 0.1.3");
            Console.WriteLine("by sephiroth99");
            Console.WriteLine("");

            if (args.Length <= 0 || args[0] == "-h" || args[0] == "-?" || args[0] == "--help")
            {
                Console.WriteLine("Extract Usage:");
                Console.WriteLine("translatr extract lang bigfile_path [be] [patch_path]");
                Console.WriteLine("");
                Console.WriteLine("Arguments:");
                Console.WriteLine(" lang        : language ID of in-game language to extract");
                Console.WriteLine(" bigfile_path: path to folder where bigfile.000 was extracted");
                Console.WriteLine(" be          : (opt) write \"be\" to enable big endian extraction");
                Console.WriteLine(" patch_path  : (opt) path to folder where patch.000 was extracted");
                Console.WriteLine("");
                Console.WriteLine("Language IDs:");
                Console.WriteLine("0 = English");
                Console.WriteLine("1 = French");
                Console.WriteLine("2 = German");
                Console.WriteLine("3 = Italian");
                Console.WriteLine("4 = Spanish");
                Console.WriteLine("5 = Japanese");
                //Console.WriteLine("6 = Portugese");
                Console.WriteLine("7 = Polish");
                //Console.WriteLine("8 = EnglishUK");
                Console.WriteLine("9 = Russian");
                Console.WriteLine("10 = Czech");
                Console.WriteLine("11 = Dutch");
                Console.WriteLine("12 = Hungarian");
                Console.WriteLine("");
                Console.WriteLine("Apply Usage:");
                Console.WriteLine("translatr apply translations_path [override_base_path [override_patch_path]]");
                Console.WriteLine("");
                Console.WriteLine("Arguments:");
                Console.WriteLine(" translations_path: path to modified translations.xml file");
                Console.WriteLine(" override_base_path: (opt) path to extracted base files. Overrides path in xml file.");
                Console.WriteLine(" override_patch_path: (opt) path to extracted patch files. Overrides path in xml file.");
                Console.WriteLine("");
                System.Environment.Exit(0);
            }
            else
            {
                if (args[0] == "apply")
                {
                    extractMode = false;
                    transPath = args[1];

                    if (args.Length > 2)
                        ovrBasePath = args[2];
                    if (args.Length > 3)
                        ovrPatchPath = args[3];
                }
                else if (args[0] != "extract")
                {
                    Console.WriteLine("Unknown usage mode \"{0}\", run \"translatr -h\" for help", args[0]);
                    System.Environment.Exit(0);
                }
                else
                {
                    if (args.Length < 3)
                    {
                        Console.WriteLine("Not enough arguments passed to program, run \"translatr -h\" for help");
                        System.Environment.Exit(0);
                    }
                    lang = int.Parse(args[1]);
                    bigfilePath = args[2];
                    if (args.Length > 3)
                        if (args[3] == "be")
                        {
                            isBigEndian = true;
                            if (args.Length > 4)
                                patchPath = args[4];
                        }
                        else
                            patchPath = args[3];
                }
            }

            if (extractMode)
                Extractor.doExtract(bigfilePath, patchPath, isBigEndian, lang);
            else
                Applicator.doApply(transPath, ovrBasePath, ovrPatchPath);
        }
    }
}
