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
        public bool isBE;

        public LocalsFile(String p, String n, bool isBE)
        {
            this.entries = new List<LocalsEntry>();
            this.sourcePath = p;
            this.name = n;
            this.isBE = isBE;
        }

        public LocalsFile(bool isBE)
            : this("", "", isBE)
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
            uint count = r.ReadUInt32();            
            if (isBE)
                count = swap(count);
            count -= 1;

            for (uint i = 0; i < count; i++)
            {
                LocalsEntry e = new LocalsEntry();

                s.Position = (i + 3)*4;

                e.index = i;
                e.offset = r.ReadUInt32();
                if (isBE)
                    e.offset = swap(e.offset);

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
            uint var = (uint)entries.Count + 1;
            w.Write(isBE ? swap(var) : var);
            ms.Position += 4;

            foreach (LocalsEntry e in entries)
            {
                w.Write(isBE ? swap(e.offset) : e.offset);
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

        public uint swap(uint u)
        {
            return (((u & 0x00FF) << 24) |
                    ((u & 0xFF00) << 8) |
                    ((u >> 8) & 0xFF00) |
                    ((u >> 24) & 0xFF));
        }
    }
}
