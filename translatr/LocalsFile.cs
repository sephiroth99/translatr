using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace translatr
{
    public class LocalsEntry
    {
        public uint index;
        public uint offset;
        public String text;
    }

    public class LocalsFile
    {
        public List<LocalsEntry> entries;
        public String sourcePath;
        public String name;

        public LocalsFile(String p, String n)
        {
            entries = new List<LocalsEntry>();
            sourcePath = p;
            name = n;
        }

        public LocalsFile()
            : this("", "")
        {
        }

        public void parse(String filePath)
        {
            this.parse(new FileStream(filePath, FileMode.Open));
        }

        public void parse(Stream s)
        {
            BinaryReader r = new BinaryReader(s);

            s.Position = 4;
            uint count = r.ReadUInt32() - 1;

            for (uint i = 0; i < count; i++)
            {
                LocalsEntry e = new LocalsEntry();

                s.Position = (i + 3)*4;

                e.index = i;
                e.offset = r.ReadUInt32();

                s.Position = e.offset;
                
                byte b;
                int len = 0;

                do
                {
                    b = r.ReadByte();
                    len++;
                }
                while (b != 0);

                s.Position = e.offset;

                e.text = Encoding.UTF8.GetString(r.ReadBytes(len - 1));

                entries.Add(e);
            }
        }

        public void rebuildAndSave(string path)
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter w = new BinaryWriter(ms);

            ms.Position = 4;
            w.Write(entries.Count + 1);
            ms.Position += 4;

            foreach (LocalsEntry e in entries)
            {
                w.Write(e.offset);
            }

            foreach (LocalsEntry e in entries)
            {
                if(e.offset == 0)
                    continue;

                ms.Position = e.offset;
                w.Write(e.text.ToCharArray());
                w.Write(0);
            }

            FileStream file = new FileStream(path, FileMode.Create);
            file.Write(ms.ToArray(), 0, (int)ms.Length);
            file.Close();
        }
    }
}
