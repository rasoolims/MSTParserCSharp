using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MSTParser
{
    public class Alphabet
    {
        private const int CurrentSerialVersion = 0;

        private readonly Dictionary<string, int> m_map;

        private bool m_growthStopped;
        private int m_numEntries;

        public Alphabet()
        {
            m_map = new Dictionary<string, int>(10000);
            m_numEntries = 0;
        }


        public int Count
        {
            get { return m_numEntries; }
        }


        // <summary> Return -1 if entry isn'T present. */
        public int LookupIndex(string entry)
        {
            const bool addIfNotPresent = true;
            if (entry == null)
                throw new ArgumentException("Can't lookup \"null\" in an Alphabet.");
            int ret=-1;

            if(m_map.ContainsKey(entry))
            {
                ret = m_map[entry];
            }
            //bool canRet = m_map.TryGetValue(entry, out ret);
            //if (!canRet)
            //{
            //    ret = -1;
            //}

            if (ret == -1 && !m_growthStopped && addIfNotPresent) 
            {
                ret = m_numEntries;
                m_map.Add(entry, ret);
                m_numEntries++;
            }
            return ret;
        }

        public string[] ToArray()
        {
            return m_map.Keys.ToArray();
        }

        public bool Contains(string entry)
        {
            return m_map.ContainsKey(entry);
        }

        public void StopGrowth()
        {
            m_growthStopped = true;
        }

        public void AllowGrowth()
        {
            m_growthStopped = false;
        }

        public bool GrowthStopped()
        {
            return m_growthStopped;
        }


        // Serialization 

        internal void WriteToStream(BinaryWriter writer)
        {
            writer.Write(CurrentSerialVersion);
            writer.Write(m_numEntries);
            writer.Write(m_map.Count);
            foreach (string obj in m_map.Keys)
            {
                writer.Write(obj);
                writer.Write(m_map[obj]);
            }
            writer.Write(m_growthStopped);
        }

        internal static Alphabet ReadFromStream(BinaryReader reader)
        {
            var alphabet = new Alphabet();
            reader.ReadInt32();
            alphabet.m_numEntries = reader.ReadInt32();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                string entry = reader.ReadString();
                int entryCount = reader.ReadInt32();
                alphabet.m_map.Add(entry, entryCount);
            }
            alphabet.m_growthStopped = reader.ReadBoolean();
            return alphabet;
        }

    }
}