using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace translatr
{
    public enum LangID
    {
        Default = -1,
        English = 0,
        French,
        German,
        Italian,
        Spanish,
        Japanese,
        Portugese,
        Polish,
        EnglishUK,
        Russian,
        Czech,
        Dutch,
        Hungarian
    };

    public class SubtitleEntry
    {
        public int blockNumber;
        public LangID lang;
        public string text;
    }

    public class CineFile
    {
        private List<SubtitleEntry> subEntries;
        private Stream dataStream;
        public String sourcePath;
        public String name;
        bool isParsed;

        public CineFile(String srcpath, String name)
        {
            this.subEntries = new List<SubtitleEntry>();
            this.dataStream = null;
            this.sourcePath = srcpath;
            this.name = name;
            this.isParsed = false;
        }

        public CineFile()
            : this("", "")
        {
        }

        public void parse(String path)
        {
            if (!isParsed)
            {
                dataStream = new FileStream(path, FileMode.Open);
                parseSubtitles();
                isParsed = true;
            }
        }

        public void add(SubtitleEntry e)
        {
            subEntries.Add(e);
        }

        public void parse(Stream s)
        {
            if (!isParsed)
            {
                dataStream = s;
                parseSubtitles();
                isParsed = true;
            }
        }

        public List<SubtitleEntry> getSubtitles()
        {
            if (isParsed)
                return subEntries;
            else
                return null;
        }

        public bool isSubs()
        {
            return (subEntries.Count > 0);
        }

        private void parseSubtitles()
        {
            int block = 0;
            UInt32 type;
            UInt32 blockSize;

            BinaryReader br = new BinaryReader(dataStream);

            // Start of MUL data
            dataStream.Position = 0x800;
            while (dataStream.Position < dataStream.Length)
            {
                // Read block header
                type = br.ReadUInt32();
                blockSize = br.ReadUInt32();
                dataStream.Position += 8;

                if (type == 1) // Cine block
                {                    
                    if (blockSize == 0x10)
                    {
                        block++;
                        dataStream.Position += 0x10;
                        continue;
                    }

                    // Copy cine block to array
                    var array = br.ReadBytes((int)blockSize);

                    parseBlock(array, block);
                    block++;
                }
                else if (type == 0) // Skip audio block
                {
                    dataStream.Position += blockSize;
                }
                else
                {
                    throw new Exception("Unknown mul block type");
                }
            }
        }

        private void parseBlock(byte[] array, int block)
        {
            // Skip first block if its a big one
            // It's most likely a cinstream so no subs here
            if (block == 0)
            {
                if (array[8] != 0x60)
                    return;
            }

            // Start at end of block
            int index = array.Length - 1;

            // Remove end zeros
            while (index >= 0 && array[index] == 0x00)
            {
                index--;
            }

            if (array[index] != 0x0d)
                return; // No subs here

            // Search for start of subtitle section
            int endidx = index;
            // 4byte align
            index -= index % 4;

            while (index > 0)
            {
                index -= 4;
                int len = ((array[index]) + (array[index + 1] << 8) + (array[index + 2] << 16) + (array[index + 3] << 24));
                if ((endidx - index - 3) == len)
                {
                    parseSubsBlock(Encoding.UTF8.GetString(array, index + 4, endidx - index - 4), block);
                    break;
                }
            }
        }

        void parseSubsBlock(String s, int block)
        {
            var ss = s.Split('\r');
            
            for (int i = 0; i < ss.Length; i += 2)
            {
                SubtitleEntry sub = new SubtitleEntry();
                sub.blockNumber = block;
                sub.lang = (LangID)(int.Parse(ss[i]));
                sub.text = ss[i + 1];

                this.subEntries.Add(sub);
            }
        }

        public void rebuild(String path)
        {
            FileStream patched = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);

            // Must have original file path to rebuild!
            if (this.sourcePath == "")
                throw new NullReferenceException("Source path is empty");

            BinaryReader br = new BinaryReader(new FileStream(this.sourcePath + this.name, FileMode.Open));

            byte[] buf;

            // Copy header
            buf = br.ReadBytes(0x800);
            patched.Write(buf, 0, buf.Length);
            
            // Do blocks
            UInt32 type;
            UInt32 len;
            UInt32 blockno = 0;
            while (br.BaseStream.Position < br.BaseStream.Length)
            {
                type = br.ReadUInt32();
                len = br.ReadUInt32();
                br.BaseStream.Position -= 8;

                if (type == 0)
                {
                    // Copy audio block as is
                    buf = br.ReadBytes((int)len + 16);
                    patched.Write(buf, 0, buf.Length);
                }
                else
                {
                    int changedEntryNo = findChangedEntry(blockno);
                    if (changedEntryNo >= 0)
                    {
                        // Change sub
                        buf = br.ReadBytes((int)len + 16);
                        buf = blockChangeSub(buf, subEntries[changedEntryNo]);
                        patched.Write(buf, 0, buf.Length);
                    }
                    else
                    {
                        // Copy cine block as is
                        buf = br.ReadBytes((int)len + 16);
                        patched.Write(buf, 0, buf.Length);
                    }
                    blockno++;
                }
            }

            patched.Close();
        }

        private int findChangedEntry(uint blockno)
        {
            for(int i = 0; i < subEntries.Count; i++)
            {
                if (subEntries[i].blockNumber == blockno)
                    return i;
            }

            return -1;
        }

        private byte[] blockChangeSub(byte[] array, SubtitleEntry entry)
        {
            int index = array.Length - 1;
            bool again = true;
            byte[] output = null;
            int zerocnt = 0;
            // Remove end zeros
            while (index >= 0 && array[index] == 0x00)
            {
                index--;
                zerocnt++;
            }
            BinaryWriter bw = null;
            int sizeDelta = 0;
            while (again)
            {
                int endpos = index;
                index--;

                // Find start of subtitle
                while (index >= 0 && array[index] != 0x0d)
                {
                    index--;
                }

                int startpos = index + 1;
                index--;

                // Find start of subtitle lang
                while ((index >= 0) && (array[index] != 0x0d) && (array[index] != 0x00))
                {
                    index--;
                }

                if (array[index] == 0x00)
                    again = false;

                int langpos = index + 1;
                
                if ((LangID)(int.Parse(Encoding.UTF8.GetString(array, langpos, startpos - langpos - 1))) == entry.lang)
                {
                    int len = Encoding.UTF8.GetByteCount(entry.text);
                    sizeDelta = len - (endpos - startpos);
                    int outarraysize =  array.Length - zerocnt + sizeDelta;
                    outarraysize = ((outarraysize + 15) >> 4) << 4;
                    output = new byte[outarraysize];

                    // Copy data before sub entry
                    Array.Copy(array, output, startpos);
                    
                    Encoding.UTF8.GetBytes(entry.text, 0, entry.text.Length, output, startpos);

                    Array.Copy(array, endpos, output, startpos + len, array.Length - zerocnt - endpos);

                    bw = new BinaryWriter(new MemoryStream(output));

                    // Fix size of mul block header                    
                    bw.Seek(4, SeekOrigin.Begin);
                    bw.Write(outarraysize - 16);
                    
                    // Fix size of cine block header
                    bw.Seek(16, SeekOrigin.Begin);
                    bw.Write(outarraysize - 20);                    
                }

                if (again == false)
                {
                    // Fix size of subs header
                    var br = new BinaryReader(new MemoryStream(array));
                    br.BaseStream.Position = index - 3;
                    var origsize = br.ReadUInt32();
                    bw.Seek(index - 3, SeekOrigin.Begin);
                    bw.Write((uint)(origsize + sizeDelta));
                    bw.Close();
                    return output;
                }
            }

            return output;
        }
    }
}
