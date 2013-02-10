using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace translatr
{
    class Extractor
    {
        private static void showHelpAndQuit()
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
            Console.WriteLine("Possible languages:");
            Console.WriteLine("English (en), French (fr), German (de), Italian (it), Spanish (es),");
            Console.WriteLine("Japanese (ja), Portugese (pt), Polish (pl), EnglishUK (uk), Russian (ru),");
            Console.WriteLine("Czech (cs), Dutch (nl), Hungarian (hu)");
            System.Environment.Exit(0);
        }

        public static void doExtract(string[] args)
        {
            bool isBigEndian = false;
            LocaleID lang = LocaleID.Default;
            String bigfilePath = String.Empty;
            String patchPath = String.Empty;

            if (args.Length < 2)
            {
                Console.WriteLine("Error! Not enough arguments passed to program.");
                Console.WriteLine("");
                showHelpAndQuit();
            }
            else
            {
                if (args[1] == "help")
                {
                    showHelpAndQuit();
                }
                else if (args.Length < 3)
                {
                    Console.WriteLine("Error! Not enough arguments passed to program.");
                    Console.WriteLine("");
                    showHelpAndQuit();
                }
                else if (args.Length > 5)
                {
                    Console.WriteLine("Error! Too many arguments passed to program.");
                    Console.WriteLine("");
                    showHelpAndQuit();
                }
                else
                {
                    lang = Locale.getFromString(args[1]);
                    if (lang == LocaleID.Default)
                    {
                        Console.WriteLine("Error! Unknown language {0}", args[1]);
                        Console.WriteLine("");
                        showHelpAndQuit();
                    }

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

            // Remove trailing backslash on paths
            if (bigfilePath.EndsWith("\\"))
                bigfilePath = bigfilePath.Remove(bigfilePath.Length - 1);

            if (patchPath.EndsWith("\\"))
                patchPath = patchPath.Remove(patchPath.Length - 1);

            // Get locale mask
            uint mask;
            if (patchPath != "")
            {
                uint mask2 = Locale.getLocaleMask(bigfilePath);
                mask = Locale.getLocaleMask(patchPath);

                if (mask2 != mask)
                    throw new Exception("Patch and bigfile locale mask different");
            }
            else
                mask = Locale.getLocaleMask(bigfilePath);

            if (mask == uint.MaxValue)
            {
                throw new Exception("Error finding localisation database (\"locals.bin\"). Make sure the game files are properly unpacked.");
            }
            else if (mask == (uint.MaxValue - 1))
            {
                Console.WriteLine("");
                Console.WriteLine("Unable to determine language of game files.");
                Console.WriteLine("Please confirm that the language {0} is supported by your game.", Locale.toString(lang));
                Console.WriteLine("NOTE: If the language is not supported by the game, you won't see any changes!");
                Console.Write("Confirm? (y/n)");
                
                var yes = new ConsoleKeyInfo('y', ConsoleKey.Y, false, false, false);
                var no = new ConsoleKeyInfo('n', ConsoleKey.N, false, false, false);
                while (true)
                {
                    var key = Console.ReadKey(true);
                    if (key == no)
                        System.Environment.Exit(0);
                    else if (key == yes)
                        break;
                }
                
                Console.WriteLine("");
            }
            //Check if lang is present in files
            else if ((mask & (1 << (int)lang)) == 0)
            {
                Console.WriteLine("");
                Console.WriteLine("Error! Language {0} not found in game files.", Locale.toString(lang));
                Console.WriteLine("");
                Console.WriteLine("Detected languages:");

                for (int i = 0; i < 16; i++)
                {
                    if((mask & (1 << i)) != 0)
                        Console.WriteLine(Locale.toString((LocaleID)i));
                }
                System.Environment.Exit(0);
            }
            
            TransFile tf = new TransFile(bigfilePath, patchPath, isBigEndian);

            var files = getFilelist(bigfilePath, patchPath, (int)lang, mask);

            System.Console.WriteLine("Searching following files for translatable text:");

            int f = 0;
            foreach (string file in files)
            {
                System.Console.WriteLine("{0} of {1} - {2}", ++f, files.Count, Path.GetFileName(file));

                if (file.EndsWith("locals.bin"))
                {
                    LocalsFile lf = new LocalsFile(isBigEndian);
                    lf.parse(file);

                    string basep;
                    string name;
                    if (file.StartsWith(bigfilePath))
                    {
                        basep = bigfilePath;
                        name = file.Substring(bigfilePath.Length);
                    }
                    else
                    {
                        basep = patchPath;
                        name = file.Substring(patchPath.Length);
                    }

                    tf.AddLocalsFile(lf, basep, name);
                }
                else
                {
                    CineFile cf = new CineFile(isBigEndian);
                    cf.parse(file);

                    if (cf.isSubs())
                    {
                        string basep;
                        string name;
                        if (file.StartsWith(bigfilePath))
                        {
                            basep = bigfilePath;
                            name = file.Substring(bigfilePath.Length);
                        }
                        else
                        {
                            basep = patchPath;
                            name = file.Substring(patchPath.Length);
                        }
                        tf.AddFile(basep, name);

                        List<SubtitleEntry> entries = cf.getSubtitles();

                        foreach (SubtitleEntry e in entries)
                        {
                            if (e.lang == lang)
                            {
                                tf.AddEntry(e.text, e.lang.ToString(), e.blockNumber.ToString());
                            }
                        }
                    }
                }
            }

            tf.Close();
            System.Console.WriteLine("Translatable text saved to file \"translations.xml\"");
        }

        private static List<String> getFilelist(String bigfilePath, String patchPath, int lang, uint mask)
        {
            List<String> patchedfiles = new List<String>();
            if (patchPath != String.Empty)
            {
                patchedfiles = searchDir(patchPath, lang, mask);
            }

            var bigfiles = searchDir(bigfilePath, lang, mask);

            // Replace patched files in main file list
            foreach (string s in patchedfiles)
            {
                //bigfiles.RemoveAll(delegate (string search) { return search == (bigfilePath + s.Remove(0, patchPath.Length)); });
                bigfiles.RemoveAll(delegate(string search) { return Path.GetFileName(search) == Path.GetFileName(s); });
                bigfiles.Add(s);
            }

            return bigfiles;
        }

        private static List<String> searchDir(String path, int lang, uint mask)
        {
            List<String> list = new List<String>();

            var dirlist = Directory.GetDirectories(path);

            foreach (string d in dirlist)
            {
                string subfolder = d.Substring(d.LastIndexOf("\\") + 1);

                if (subfolder == "default")
                {
                    //We may have a locals.bin in the default folder (ex. russian version)
                    //In that case there shouldn't be one in the locale folder
                    var localsfiles = Directory.GetFiles(d, "locals.bin", SearchOption.AllDirectories);
                    if (localsfiles.Length > 0)
                    {
                        list.AddRange(localsfiles); // Should only have one!
                    }

                    var files = Directory.GetFiles(d, "*.mul", SearchOption.AllDirectories);
                    if (files.Length > 0)
                    {
                        list.AddRange(files);
                    }
                }
                else
                {
                    int folderLocale = Int32.Parse(subfolder, System.Globalization.NumberStyles.HexNumber);
                    int locale = (1 << lang);

                    if (((folderLocale & mask) & locale) != 0)
                    {
                        // Get mul files
                        var files = Directory.GetFiles(d, "*.mul", SearchOption.AllDirectories);
                        if (files.Length > 0)
                        {
                            list.AddRange(files);
                        }

                        var localsfiles = Directory.GetFiles(d, "locals.bin", SearchOption.AllDirectories);
                        if (localsfiles.Length > 0)
                        {
                            list.AddRange(localsfiles); // Should only have one!
                        }
                    }
                }
            }

            return list;
        }
    }
}
