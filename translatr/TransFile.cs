using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.Xml.XPath;

namespace translatr
{
    class TransFile
    {
        private XmlWriter xml;
        private bool isFileEntryOpen;

        public TransFile(String bigpath, String patchpath, bool isBE)
        {
            var xmlsettings = new XmlWriterSettings();
            xmlsettings.Indent = true;

            xml = XmlWriter.Create("translations.xml", xmlsettings);
            
            xml.WriteStartDocument();
            xml.WriteStartElement("root");
            xml.WriteAttributeString("bigpath", bigpath);
            xml.WriteAttributeString("patchpath", patchpath);
            xml.WriteAttributeString("bigendian", isBE ? "true" : "false");

            isFileEntryOpen = false;
        }

        public static void Open(String inputPath, out LocalsFile lf, out List<CineFile> cfl, out String bigPath, out String patchPath)
        {
            lf = null;
            cfl = new List<CineFile>();
            bool isBigEndian;
            
            var doc = new XPathDocument(inputPath, XmlSpace.Preserve);
            var nav = doc.CreateNavigator();

            var root = nav.SelectSingleNode("/root");

            bigPath = root.GetAttribute("bigpath", "");
            patchPath = root.GetAttribute("patchpath", "");
            var be = root.GetAttribute("bigendian", "");
            if (be == string.Empty)
                isBigEndian = false;
            else
                isBigEndian = bool.Parse(be);

            var fileNodes = root.Select("file");
            while (fileNodes.MoveNext() == true)
            {
                addCineFileNode(fileNodes.Current, cfl, isBigEndian);
            }

            var localsNode = root.Select("locals");
            localsNode.MoveNext();
            addLocalsFileNode(localsNode.Current, out lf, isBigEndian);
        }

        private static void addLocalsFileNode(XPathNavigator current, out LocalsFile lf, bool isBigEndian)
        {
            string basepath = current.GetAttribute("base", "");
            string name = current.GetAttribute("name", "");
            lf = new LocalsFile(basepath, name, isBigEndian);
            lf.id = uint.Parse(current.GetAttribute("id", ""));

            uint index, offset;
            String entryText = "";
            uint prevIndex = 0;
            uint lastOffset = 0;
            
            var entryNodes = current.Select("entry");
            entryNodes.MoveNext();
            prevIndex = uint.Parse(entryNodes.Current.GetAttribute("index", ""));
            lastOffset = uint.Parse(entryNodes.Current.GetAttribute("offset", ""));
            entryText = entryText + entryNodes.Current.Value;

            while (entryNodes.MoveNext() == true)
            {
                // Get index of entry
                index = uint.Parse(entryNodes.Current.GetAttribute("index", ""));
                offset = uint.Parse(entryNodes.Current.GetAttribute("offset", ""));

                if (prevIndex == index)
                {
                    // this entry is the same as the last one, save the text with LF
                    entryText = entryText + "\n" + entryNodes.Current.Value;
                    continue;
                }

                // This entry is a new one, save last entry
                LocalsEntry e = new LocalsEntry();
                e.index = prevIndex;
                e.offset = lastOffset;
                lastOffset += (uint)Encoding.UTF8.GetByteCount(entryText) + 1;
                /*if (lastOffset != offset)
                    throw new Exception("Error loading locals.bin");*/
                e.text = entryText;

                lf.entries.Add(e);

                // Save text of current entry
                entryText = entryNodes.Current.Value;
                prevIndex++;

                // If next entry is not sequential, we need to add empty entries
                while (index > prevIndex)
                {
                    LocalsEntry fake = new LocalsEntry();
                    fake.index = prevIndex;
                    fake.offset = 0;
                    fake.text = "";
                    lf.entries.Add(fake);
                    prevIndex++;
                }
            }

            // Write last entry
            LocalsEntry last = new LocalsEntry();
            last.index = prevIndex;
            last.offset = lastOffset;
            last.text = entryText;
            lf.entries.Add(last);
        }

        private static void addCineFileNode(XPathNavigator current, List<CineFile> cfl, bool isBigEndian)
        {
            string basepath = current.GetAttribute("base", "");
            string name = current.GetAttribute("name", "");

            var entryNodes = current.Select("entry");

            CineFile cf = new CineFile(basepath, name, isBigEndian);
            SubtitleEntry se;

            // Get first entry
            entryNodes.MoveNext();
            se = new SubtitleEntry();
            se.lang = Locale.getFromString(entryNodes.Current.GetAttribute("lang", ""));
            se.blockNumber = int.Parse(entryNodes.Current.GetAttribute("block", ""));
            se.text = entryNodes.Current.Value;

            while (entryNodes.MoveNext() == true)
            {
                // Check if same block
                if (se.blockNumber == int.Parse(entryNodes.Current.GetAttribute("block", "")))
                {
                    se.text += ("\n" + entryNodes.Current.Value);
                }
                else
                {
                    // Save previous entry
                    cf.add(se);

                    // Save new current entry
                    se = new SubtitleEntry();
                    se.lang = Locale.getFromString(entryNodes.Current.GetAttribute("lang", ""));
                    se.blockNumber = int.Parse(entryNodes.Current.GetAttribute("block", ""));
                    se.text = entryNodes.Current.Value;
                }
            }

            // Add last sub entry
            cf.add(se);

            // Add cinefile to list
            cfl.Add(cf);
        }

        public void Close()
        {
            xml.WriteEndElement();
            xml.WriteEndDocument();
            xml.Flush();
            xml.Close();
        }

        public void AddFile(String basepath, String filename)
        {
            if (isFileEntryOpen)
            {
                isFileEntryOpen = false;
                xml.WriteEndElement();
            }

            xml.WriteStartElement("file");
            xml.WriteAttributeString("base", basepath);
            xml.WriteAttributeString("name", filename);
            isFileEntryOpen = true;
        }

        public void AddEntry(String text, String language, String blockNb)
        {
            var lines = text.Split("\n".ToCharArray());

            foreach (string line in lines)
            {
                xml.WriteComment(line);

                xml.WriteStartElement("entry");
                xml.WriteAttributeString("lang", language);
                xml.WriteAttributeString("block", blockNb);
                xml.WriteValue(line);
                xml.WriteEndElement();
            }            
        }

        public void AddLocalsFile(LocalsFile lf, String basepath, String filename)
        {
            if (isFileEntryOpen)
            {
                isFileEntryOpen = false;
                xml.WriteEndElement();
            }

            xml.WriteStartElement("locals");
            xml.WriteAttributeString("id", lf.id.ToString());
            xml.WriteAttributeString("base", basepath);
            xml.WriteAttributeString("name", filename);
            foreach (LocalsEntry e in lf.entries)
            {
                if (e.text != string.Empty)
                {
                    var lines = e.text.Split("\n".ToCharArray());
                    foreach (string line in lines)
                    {
                        xml.WriteComment(line);

                        xml.WriteStartElement("entry");
                        xml.WriteAttributeString("index", e.index.ToString());
                        xml.WriteAttributeString("offset", e.offset.ToString());
                        xml.WriteValue(line);
                        xml.WriteEndElement();
                    }
                }
            }

            xml.WriteEndElement();
        }
    }
}
