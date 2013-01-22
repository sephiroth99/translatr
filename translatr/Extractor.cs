using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace translatr
{
    class Extractor
    {
        public static void doExtract(String bigfilePath, String patchPath, bool isBigEndian, int lang)
        {
            TransFile tf = new TransFile(bigfilePath, patchPath, isBigEndian, lang);

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
                    tf.AddFile(basep, name);

                    foreach (LocalsEntry e in lf.entries)
                    {
                        // Watch out not the same info as the attribute name!
                        if (e.text != string.Empty)
                            tf.AddEntry(e.text, e.index.ToString(), e.offset.ToString());
                    }
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
                            if (e.lang == (LangID)lang)
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

        /*private static LangID getLocale(Int32 code)
        {
            if (code == (Int32)LangID.Default)
            {
                return LangID.Default;
            }

            int i = 0;
            while ((code & (Int32)(1 << i)) == 0)
                i++;

            return (LangID)i;
        }*/

        private static int getLocaleMask(int lang)
        {
            LangID l = (LangID)lang;

            if (l == LangID.Russian)
                return 0x0200;
            else if (l == LangID.Japanese)
                return 0x0020;
            else if (l == LangID.Polish || l == LangID.Czech || l == LangID.Hungarian)
                return 0x1480;
            else if (l == LangID.English || l == LangID.French || l == LangID.Italian || l == LangID.German || l == LangID.Spanish || l == LangID.Dutch)
                return 0x081F;
            else
                return 0;
        }
    }
}
