using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Xml.XPath;

namespace translatr
{
    class Applicator
    {
        private static void showHelpAndQuit()
        {
            Console.WriteLine("Apply Usage:");
            Console.WriteLine("translatr apply translations_path [override_base_path [override_patch_path]]");
            Console.WriteLine("");
            Console.WriteLine("Arguments:");
            Console.WriteLine(" translations_path: path to modified translations.xml file");
            Console.WriteLine(" override_base_path: (opt) path to extracted base files. Overrides path in xml file.");
            Console.WriteLine(" override_patch_path: (opt) path to extracted patch files. Overrides path in xml file.");
            System.Environment.Exit(0);
        }

        public static void doApply(string[] args)
        {
            String transFilePath = String.Empty;
            String ovrFileBasePath = String.Empty;
            String ovrFilePatchPath = String.Empty;

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
                else if (args.Length > 4)
                {
                    Console.WriteLine("Error! Too many arguments passed to program.");
                    Console.WriteLine("");
                    showHelpAndQuit();
                }

                transFilePath = args[1];

                if (args.Length > 2)
                    ovrFileBasePath = args[2];
                if (args.Length > 3)
                    ovrFilePatchPath = args[3];
            }

            LocalsFile localsFile = null;
            List<CineFile> cineFileList = null;

            String patchPathBase, origPatchPathBase;
            String bigPathBase, origBigPathBase;
            String outPath = "newpatch";
            String dest;

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
                    // Add node in file
                    xdoc.DocumentElement.AppendChild(createNode(xdoc, localsFile.name));
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
                        // Add node in file
                        xdoc.DocumentElement.AppendChild(createNode(xdoc, cinefile.name));
                    }
                }
            }

            System.Console.WriteLine("done!");

            // Add new files in patch dir to bigfile.xml
            System.Console.Write("Adding new files to \"bigfile.xml\"...");
            xdoc.Save(outPath + "\\bigfile.xml");
            System.Console.WriteLine("done!");
        }

        private static XmlNode createNode(XmlDocument xdoc, string name)
        {
            char[] separator = new char[1];
            separator[0] = '\\';
            var separatedPath = name.Split(separator, StringSplitOptions.RemoveEmptyEntries);

            // Create new entry node
            XmlNode n = xdoc.CreateNode(XmlNodeType.Element, "entry", null);
            if (name.StartsWith("\\"))
                n.InnerText = name.Substring(1);
            else
                n.InnerText = name;

            // Add hash attribute
            var hashXmlAttr = xdoc.CreateAttribute("hash");
            if (separatedPath[1] == "__UNKNOWN")
                hashXmlAttr.InnerText = Path.GetFileNameWithoutExtension(separatedPath[separatedPath.Length - 1]);
            else
            {
                int startOffset = separatedPath[0].Length + 1;

                // This is assuming that there will be no files which the longFileName start with \
                // even though they exist. They shouldn't contain subs or text so we are ok.
                if (name.StartsWith("\\"))
                    startOffset++;

                var longFileName = Encoding.ASCII.GetBytes(name.Substring(startOffset));
                hashXmlAttr.InnerText = hasher(longFileName).ToString("X8").ToUpper();
            }
            n.Attributes.Append(hashXmlAttr);

            // Add locale attribute
            var localeXmlAttr = xdoc.CreateAttribute("locale");
            if (separatedPath[0] == "default")
                localeXmlAttr.InnerText = "FFFFFFFF";
            else
                localeXmlAttr.InnerText = separatedPath[0];
            n.Attributes.Append(localeXmlAttr);

            return n;
        }

        private static Int32 hasher(byte[] s)
        {
            Int32 hash = -1;
            const Int32 poly = 0x04C11DB7;

            if (s.Length <= 0)
                return hash;

            for (int i = 0; i < s.Length; i++)
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
