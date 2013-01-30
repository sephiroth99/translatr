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
            Console.WriteLine("Possible language IDs:");
            Console.WriteLine("0 = English");
            Console.WriteLine("1 = French");
            Console.WriteLine("2 = German");
            Console.WriteLine("3 = Italian");
            Console.WriteLine("4 = Spanish");
            Console.WriteLine("5 = Japanese");
            Console.WriteLine("6 = Portugese");
            Console.WriteLine("7 = Polish");
            Console.WriteLine("8 = EnglishUK");
            Console.WriteLine("9 = Russian");
            Console.WriteLine("10 = Czech");
            Console.WriteLine("11 = Dutch");
            Console.WriteLine("12 = Hungarian");
            System.Environment.Exit(0);
        }

        public static void doExtract(string[] args)
        {
            bool isBigEndian = false;
            int lang = 1;
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

            TransFile tf = new TransFile(bigfilePath, patchPath, isBigEndian);

            var files = getFilelist(bigfilePath, patchPath, lang);

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
                            if (e.lang == (LocaleID)lang)
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

        private static List<String> getFilelist(String bigfilePath, String patchPath, int lang)
        {
            List<String> patchedfiles = new List<String>();
            if (patchPath != String.Empty)
            {
                patchedfiles = searchDir(patchPath, lang);
            }

            var bigfiles = searchDir(bigfilePath, lang);

            // Replace patched files in main file list
            foreach (string s in patchedfiles)
            {
                //bigfiles.RemoveAll(delegate (string search) { return search == (bigfilePath + s.Remove(0, patchPath.Length)); });
                bigfiles.RemoveAll(delegate(string search) { return Path.GetFileName(search) == Path.GetFileName(s); });
                bigfiles.Add(s);
            }

            return bigfiles;
        }

        private static List<String> searchDir(String path, int lang)
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
                    int mask = getLocaleMask(lang);
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

        private static int getLocaleMask(int lang)
        {
            LocaleID l = (LocaleID)lang;

            if (l == LocaleID.Russian)
                return 0x0200;
            else if (l == LocaleID.Japanese)
                return 0x0020;
            else if (l == LocaleID.Polish || l == LocaleID.Czech || l == LocaleID.Hungarian)
                return 0x1480;
            else if (l == LocaleID.English || l == LocaleID.French || l == LocaleID.Italian || l == LocaleID.German || l == LocaleID.Spanish || l == LocaleID.Dutch)
                return 0x081F;
            else
                return 0;
        }
    }
}
