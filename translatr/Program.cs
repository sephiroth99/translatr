using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Text;
using System.Xml.XPath;

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
                Console.WriteLine("6 = Portugese");
                Console.WriteLine("7 = Polish");
                Console.WriteLine("8 = EnglishUK");
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
                doExtract(bigfilePath, patchPath, isBigEndian, lang);
            else
                doApply(transPath, ovrBasePath, ovrPatchPath);
        }

        private static void doExtract(String bigfilePath, String patchPath, bool isBigEndian, int lang)
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
                        if(e.text != string.Empty)
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

        private static void doApply(String transFilePath, String ovrFileBasePath, String ovrFilePatchPath)
        {
            LocalsFile localsFile = null;
            List<CineFile> cineFileList = null;
            
            String patchPathBase, origPatchPathBase;
            String bigPathBase, origBigPathBase;
            String outPath = "newpatch";
            String dest;
            int lang;

            Directory.CreateDirectory(outPath);
            
            origBigPathBase = String.Empty;
            origPatchPathBase = String.Empty;

            // Get info from translations.xml file
            System.Console.Write("Loading translation data...");
            TransFile.Open(transFilePath, out localsFile, out cineFileList, out bigPathBase, out patchPathBase);
            System.Console.WriteLine("done!");

            if (ovrFileBasePath != String.Empty)
            {
                origBigPathBase = bigPathBase;
                bigPathBase = ovrFileBasePath;

                if (patchPathBase != String.Empty)
                {
                    if (ovrFilePatchPath == String.Empty)
                    {
                        System.Console.WriteLine("Error: A patch was specified in the translation file\nPatch override path must be provided!");
                        System.Environment.Exit(-1);
                    }
                    else
                    {
                        origPatchPathBase = patchPathBase;
                        patchPathBase = ovrFilePatchPath;
                    }
                }
                System.Console.WriteLine("Using different source paths");
                System.Console.WriteLine("Base: {0}", bigPathBase);
                if (patchPathBase != String.Empty)
                    System.Console.WriteLine("Patch: {0}", patchPathBase);
                else
                    System.Console.WriteLine("");
            }

            // Copy existing patch into output dir
            System.Console.Write("Copying existing patch data...");
            if (patchPathBase != "")
            {
                foreach (string filename in Directory.GetFiles(patchPathBase, "*.*", SearchOption.AllDirectories))
                {
                    dest = outPath + filename.Substring(patchPathBase.Length);
                    Directory.CreateDirectory(Path.GetDirectoryName(dest));
                    File.Copy(filename, dest, true);
                }
                System.Console.WriteLine("done!");
            }
            else
            {
                System.Console.WriteLine("No patch specified, skipping!");

                //Creating empty bigfile.xml
                var xmlsettings = new XmlWriterSettings();
                xmlsettings.Indent = true;
                XmlWriter xml = XmlWriter.Create(outPath + "\\bigfile.xml", xmlsettings);

                // Open main files bigfile.xml to get values
                var doc = new XPathDocument(bigPathBase + "\\bigfile.xml");
                var nav = doc.CreateNavigator();
                var root = nav.SelectSingleNode("/files");

                xml.WriteStartDocument();
                xml.WriteStartElement("files");
                xml.WriteAttributeString("alignment", root.GetAttribute("alignment", ""));
                xml.WriteAttributeString("endian", root.GetAttribute("endian", ""));
                xml.WriteEndElement();
                xml.WriteEndDocument();
                xml.Flush();
                xml.Close();
            }

            XmlDocument xdoc = new XmlDocument();
            xdoc.Load(outPath + "\\bigfile.xml");
            
            // Create new files in out dir
            System.Console.WriteLine("Creating translated files...");
            
            // locals.bin
            if (localsFile != null)
            {
                dest = outPath + localsFile.name;
                Directory.CreateDirectory(Path.GetDirectoryName(dest));

                if (localsFile.sourcePath == origBigPathBase)
                    localsFile.sourcePath = bigPathBase;
                else if (localsFile.sourcePath == origPatchPathBase)
                    localsFile.sourcePath = patchPathBase;
                
                localsFile.rebuildAndSave(dest);
                System.Console.WriteLine(dest);

                if (localsFile.sourcePath.StartsWith(bigPathBase))
                {
                    // Create new entry node
                    XmlNode n = xdoc.CreateNode(XmlNodeType.Element, "entry", null);
                    n.InnerText = localsFile.name.Substring(1);

                    // Add hash attribute
                    var hashXmlAttr = xdoc.CreateAttribute("hash");
                    hashXmlAttr.InnerText = hasher(Encoding.ASCII.GetBytes(localsFile.name.Substring(10).ToCharArray())).ToString("X").ToUpper();
                    n.Attributes.Append(hashXmlAttr);

                    // Add locale attribute
                    var localeXmlAttr = xdoc.CreateAttribute("locale");
                    localeXmlAttr.InnerText = localsFile.name.Substring(1, 8);
                    n.Attributes.Append(localeXmlAttr);

                    // Add node in file
                    xdoc.DocumentElement.AppendChild(n);
                }
            }

            // mul files
            if (cineFileList.Count > 0)
            {
                foreach (CineFile cinefile in cineFileList)
                {
                    dest = outPath + cinefile.name;
                    Directory.CreateDirectory(Path.GetDirectoryName(dest));

                    // Replace the source path for the override one
                    // If no override, orig*PathBase will be empty
                    if (cinefile.sourcePath == origBigPathBase)
                        cinefile.sourcePath = bigPathBase;
                    else if (cinefile.sourcePath == origPatchPathBase)
                        cinefile.sourcePath = patchPathBase;

                    cinefile.rebuild(dest);
                    System.Console.WriteLine(dest);

                    if (cinefile.sourcePath.StartsWith(bigPathBase))
                    {
                        // Create new entry node
                        XmlNode n = xdoc.CreateNode(XmlNodeType.Element, "entry", null);
                        n.InnerText = cinefile.name.Substring(1);

                        // Add hash attribute
                        var hashXmlAttr = xdoc.CreateAttribute("hash");
                        hashXmlAttr.InnerText = hasher(Encoding.ASCII.GetBytes(cinefile.name.Substring(10).ToCharArray())).ToString("X").ToUpper();
                        n.Attributes.Append(hashXmlAttr);
                        
                        // Add locale attribute
                        var localeXmlAttr = xdoc.CreateAttribute("locale");
                        localeXmlAttr.InnerText = cinefile.name.Substring(1, 8);
                        n.Attributes.Append(localeXmlAttr);

                        // Add node in file
                        xdoc.DocumentElement.AppendChild(n);
                    }
                }
            }

            System.Console.WriteLine("done!");

            // Add new files in patch dir to bigfile.xml
            System.Console.Write("Adding new files to \"bigfile.xml\"...");
            xdoc.Save(outPath + "\\bigfile.xml");
            System.Console.WriteLine("done!");
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
                bigfiles.RemoveAll(delegate (string search) { return Path.GetFileName(search) == Path.GetFileName(s); });
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
                    continue;
                }
                else if (getLocale(Int32.Parse(subfolder, System.Globalization.NumberStyles.HexNumber)) != (LangID)lang)
                {
                    continue;
                }
                else
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

            return list;
        }

        private static LangID getLocale(Int32 code)
        {
            if (code == (Int32)LangID.Default)
            {
                return LangID.Default;
            }

            int i = 0;
            while ((code & (Int32)(1 << i)) == 0)
                i++;

            return (LangID)i;
        }

        private static Int32 hasher(byte[] s)
        {
            Int32 hash = -1;
            const Int32 poly = 0x04C11DB7;

            if (s.Length <= 0)
                return hash;

            for(int i = 0; i < s.Length; i++)
            {
                hash ^= (s[i] << 0x18);

                for (int iter = 0; iter < 8; iter++)
                {
                    if (hash >= 0)
                    {
                        hash += hash;
                    }
                    else
                    {
                        hash += hash;
                        hash ^= poly;
                    }
                }
            }

            hash ^= -1;

            return hash;
        }
    }
}
