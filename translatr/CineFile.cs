using System;
using System.IO;
using System.ComponentModel;
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
        bool isBE;

        public CineFile(String srcpath, String name, bool isBE)
        {
            this.subEntries = new List<SubtitleEntry>();
            this.dataStream = null;
            this.sourcePath = srcpath;
            this.name = name;
            this.isParsed = false;
            this.isBE = isBE;
        }

        public CineFile(bool isBE)
            : this("", "", isBE)
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
                type = dataStream.readuint(isBE);
                blockSize = dataStream.readuint(isBE);
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

        public void rebuild(String path)
        {
            FileStream patched = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);

            // Must have original file path to rebuild!
            if (this.sourcePath == "")
                throw new NullReferenceException("Source path is empty");

            var s = new FileStream(this.sourcePath + this.name, FileMode.Open);
            BinaryReader br = new BinaryReader(s);

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
                type = s.readuint(isBE);
                len = s.readuint(isBE);
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
                        buf = blockChangeSub(buf, subEntries[changedEntryNo], (int)blockno);
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

            int startidx = findSubsStartIndex(array, endidx);

            if (startidx == 0)
                return; //no subs found
            else
                subEntries = parseSubsBlock(Encoding.UTF8.GetString(array, startidx + 4, endidx - startidx - 4), block);
        }

        int findSubsStartIndex(byte[] array, int endidx)
        {
            int index = endidx;
            index -= (index % 4);

            while (index > 3)
            {
                index -= 4;
                uint len = BitConverter.ToUInt32(array, index);
                if (isBE)
                    len = len.swap();

                if ((endidx - index - 3) == len)
                {                    
                    break;
                }
            }

            return index;
        }

        static List<SubtitleEntry> parseSubsBlock(String s, int block)
        {
            List<SubtitleEntry> entries = new List<SubtitleEntry>();

            var ss = s.Split('\r');
            
            for (int i = 0; i < ss.Length; i += 2)
            {
                SubtitleEntry sub = new SubtitleEntry();
                sub.blockNumber = block;
                sub.lang = (LangID)(int.Parse(ss[i]));
                sub.text = ss[i + 1];

                entries.Add(sub);
            }

            return entries;
        }

        byte[] rebuildSubsBlock(List<SubtitleEntry> entries, int blockNumber)
        {
            MemoryStream mem = new MemoryStream();
            BinaryWriter br = new BinaryWriter(mem);

            mem.Position += 4;

            foreach (SubtitleEntry e in entries)
            {
                if (e.blockNumber != blockNumber)
                    continue;

                br.Write((byte)((byte)e.lang + 0x30));
                br.Write('\r');
                br.Write(Encoding.UTF8.GetBytes(e.text));
                br.Write('\r');
            }

            mem.Position = 0;
            mem.writeuint((uint)(mem.Length - 4), isBE);

            return mem.ToArray();
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

        private byte[] blockChangeSub(byte[] array, SubtitleEntry entry, int blockno)
        {
            int index = array.Length - 1;
            byte[] output = null;

            // Remove end zeros
            while (index >= 0 && array[index] == 0x00)
            {
                index--;
            }

            int endidx = index;
            int startidx = findSubsStartIndex(array, endidx);
            if (startidx == 0)
                throw new Exception("Error finding sub to replace");

            // Read original subs
            var origSubs = parseSubsBlock(Encoding.UTF8.GetString(array, startidx + 4, endidx - startidx - 4), blockno);

            // Replace new subtitle text
            foreach (SubtitleEntry newEntry in subEntries)
            {
                if (newEntry.blockNumber != blockno)
                    continue;

                foreach (SubtitleEntry origEntry in origSubs)
                {
                    if (newEntry.lang == origEntry.lang)
                    {
                        origEntry.text = newEntry.text;
                        break;
                    }
                }
            }

            // Get the modified subs block
            var subs = rebuildSubsBlock(origSubs, blockno); //origSubs being the new subs

            // Compute total length
            int length = startidx + subs.Length;
            // Align to 16 bytes
            length = ((length + 15) >> 4) << 4;

            output = new byte[length];

            // Copy data befor subs
            Array.Copy(array, output, startidx);

            // Copy subs
            Array.Copy(subs, 0, output, startidx, subs.Length);

            MemoryStream ms = new MemoryStream(output);
            ms.Position = 4;
            ms.writeuint((uint)(length - 16), isBE);
            ms.Position = 16;
            ms.writeuint((uint)(length - 20), isBE);

            return output;
        }
    }
}
