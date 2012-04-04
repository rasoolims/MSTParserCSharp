using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MSTParser;
using MSTParser.Extensions;

namespace MSTParser
{
    public class DependencyPipe
    {
        public bool CreateForest ;
        public Alphabet DataAlphabet;

        public bool Labeled ;

        public Alphabet TypeAlphabet;
        public string[] Types ;
        public int[] TypesInt ;

        public DependencyPipe() : this(true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyPipe"/> class.
        /// </summary>
        /// <param name="createForest">if set to <c>true</c> [create forest].</param>
        public DependencyPipe(bool createForest)
        {
            DataAlphabet = new Alphabet();
            TypeAlphabet = new Alphabet();
            CreateForest = createForest;
        }

        public void SetLabeled(string file)
        {
            var sr = File.OpenText(file);
            sr.ReadLine();
            sr.ReadLine();
            sr.ReadLine();
            var line = sr.ReadLine();
            if (line.Trim().Length > 0) Labeled = true;
            sr.Close();
        }
        public void setLabel(bool label)
        {
            Labeled = label;
        }
        public string[][] ReadLines(StreamReader reader)
        {
            string line = reader.ReadLine();
            string posLine = reader.ReadLine();
            string labLine = Labeled ? reader.ReadLine() : posLine;
            string depsLine = reader.ReadLine();
            reader.ReadLine(); // blank line

            if (line == null) return null;

            string[] toks = line.Split("\t".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            string[] pos = posLine.Split("\t".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            string[] labs = labLine.Split("\t".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            string[] deps = depsLine.Split("\t".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            var toksNew = new string[toks.Length + 1];
            var posNew = new string[pos.Length + 1];
            var labsNew = new string[labs.Length + 1];
            var depsNew = new string[deps.Length + 1];
            toksNew[0] = "<root>";
            posNew[0] = "<root-POS>";
            labsNew[0] = "<no-Type>";
            depsNew[0] = "-1";
            for (int i = 0; i < toks.Length; i++)
            {
                toksNew[i + 1] = Normalize(toks[i]);
                posNew[i + 1] = pos[i];
                labsNew[i + 1] = Labeled ? labs[i] : "<no-Type>";
                depsNew[i + 1] = deps[i];
            }
            toks = toksNew;
            pos = posNew;
            labs = labsNew;
            deps = depsNew;

            var result = new string[4][];
            result[0] = toks;
            result[1] = pos;
            result[2] = labs;
            result[3] = deps;
            return result;
        }
      
        public void ReadLines(ref string []toks,ref string [] pos,out string[] labels,out int []deps)
        {

            var toksNew = new string[toks.Length + 1];
            var posNew = new string[pos.Length + 1];
            var labsNew = new string[pos.Length + 1];
            var depsNew = new int[pos.Length + 1];
            toksNew[0] = "<root>";
            posNew[0] = "<root-POS>";
            labsNew[0] = "<no-Type>";
            depsNew[0] = -1;
            for (int i = 0; i < toks.Length; i++)
            {
                toksNew[i + 1] = Normalize(toks[i]);
                posNew[i + 1] = pos[i];
                labsNew[i + 1] = "<no-Type>";
                depsNew[i + 1] = -1;
            }

            toks = toksNew;
            pos = posNew;
            labels = labsNew;
           deps = depsNew;
        }
        public DependencyInstance CreateInstance(StreamReader reader)
        {
            string[][] lines = ReadLines(reader);
            if (lines == null) return null;

            string[] toks = lines[0];
            string[] pos = lines[1];
            string[] labs = lines[2];
            string[] deps = lines[3];

            var deps1 = new int[deps.Length];
            for (int i = 0; i < deps.Length; i++)
                deps1[i] = int.Parse(deps[i]);

            FeatureVector fv = CreateFeatureVector(toks, pos, labs, deps1);

            var pti = new DependencyInstance(toks, pos, labs, fv);

            string spans = "";
            for (int i = 1; i < deps.Length; i++)
            {
                spans += deps[i] + "|" + i + ":" + TypeAlphabet.LookupIndex(labs[i]) + " ";
            }
            pti.ActParseTree = spans.Trim();

            return pti;
        }

        public DependencyInstance CreateInstance(ref string[] toks,ref string[] pos,out string []labs, out int[] deps)
        {
            ReadLines(ref toks,ref pos, out labs, out deps);

            FeatureVector fv = CreateFeatureVector(toks, pos, labs, deps);

            var pti = new DependencyInstance(toks, pos, labs, fv);

            string spans = "";
            for (int i = 1; i < deps.Length; i++)
            {
                spans += deps[i]+"|" + i + ":"+TypeAlphabet.LookupIndex(labs[i])+" ";
            }
            pti.ActParseTree = spans.Trim();

            return pti;
        }
        public DependencyInstance[] CreateInstances(string fileName,
                                                    string featFileName)
        {
            CreateAlphabet(fileName);

            Console.WriteLine("Num Features: " + DataAlphabet.Count);

            var reader = 
                new StreamReader(new FileStream(fileName, FileMode.Open), Encoding.UTF8);
            string[][] lines = ReadLines(reader);

            var lt = new List<object>();

            BinaryWriter bWriter = CreateForest
                                    ? new BinaryWriter(new FileStream(featFileName, FileMode.Create)) //In doubt
                                    : null;

            int num1 = 0;
            while (lines != null)
            {
          //      Console.WriteLine("Creating Feature Vector Instance: " + num1);

                string[] toks = lines[0];
                string[] pos = lines[1];
                string[] labs = lines[2];
                string[] deps = lines[3];

                var deps1 = new int[deps.Length];
                for (int i = 0; i < deps.Length; i++)
                    deps1[i] = int.Parse(deps[i]);

                FeatureVector fv = CreateFeatureVector(toks, pos, labs, deps1);

                var pti_ = new DependencyInstance(toks, pos, labs, fv);

                string spans = "";
                for (int i = 1; i < deps.Length; i++)
                {
                    spans += deps[i] + "|" + i + ":" + TypeAlphabet.LookupIndex(labs[i]) + " ";
                }
                pti_.ActParseTree = spans.Trim();

                if (CreateForest)
                    WritePossibleFeatures(pti_, bWriter);
                pti_ = null;

                lt.Add(new DependencyInstance(toks.Length));

                lines = ReadLines(reader);
                num1++;
            }

            CloseAlphabets();

            var pti = new DependencyInstance[lt.Count];
            for (int i = 0; i < pti.Length; i++)
            {
                pti[i] = (DependencyInstance) lt[i];
            }

            if (CreateForest)
                bWriter.Close();

            reader.Close();

            return pti;
        }

        private void CreateAlphabet(string file)
        {
            Console.Write("Creating Alphabet ... ");

            var reader =
                new StreamReader(new FileStream(file, FileMode.Open), Encoding.UTF8);
            var lines = ReadLines(reader);

            int cnt = 0;

            while (lines != null)
            {
                var toks = lines[0];
                var pos = lines[1];
                var labs = lines[2];
                var deps = lines[3];

                for (int i = 0; i < labs.Length; i++)
                    TypeAlphabet.LookupIndex(labs[i]);

                var deps1 = new int[deps.Length];
                for (int i = 0; i < deps.Length; i++)
                {
                    deps1[i] = int.Parse(deps[i]);
                }

                CreateFeatureVector(toks, pos, labs, deps1);

                lines = ReadLines(reader);
                cnt++;
            }

            CloseAlphabets();

            reader.Close();

            Console.WriteLine("Done.");
        }

        public void CloseAlphabets()
        {
            DataAlphabet.StopGrowth();
            TypeAlphabet.StopGrowth();

            Types = new string[TypeAlphabet.Count];
            string[] keys = TypeAlphabet.ToArray();
            Array.Sort(keys);

            for (int i = 0; i < keys.Length; i++)
            {
                int indx = TypeAlphabet.LookupIndex(keys[i]);
                Types[indx] = (string) keys[i];
            }

            KBestParseForest.RootType = TypeAlphabet.LookupIndex("<root-Type>");
        }

        public string Normalize(string s)
        {
            if (s.Matches(@"[0-9]+\.[0-9]+|[0-9]+[0-9,]+|[0-9]+"))
                return "<num>";

            return s;
        }

        public virtual FeatureVector CreateFeatureVector(string[] toks,
                                                 string[] pos,
                                                 string[] posA,
                                                 int small,
                                                 int large,
                                                 bool attR,
                                                 FeatureVector fv)
        {
            string att = "";
            att = attR ? "RA" : "LA";

            int dist = Math.Abs(large - small);
            string distBool = "0";
            if (dist > 10)
                distBool = "10";
            else if (dist > 5)
                distBool = "5";
            else if (dist > 4)
                distBool = "4";
            else if (dist > 3)
                distBool = "3";
            else if (dist > 2)
                distBool = "2";
            else if (dist > 1)
                distBool = "1";

            string attDist = "&" + att + "&" + distBool;

            string pLeft = small > 0 ? pos[small - 1] : "STR";
            string pRight = large < pos.Length - 1 ? pos[large + 1] : "END";
            string pLeftRight = small < large - 1 ? pos[small + 1] : "MID";
            string pRightLeft = large > small + 1 ? pos[large - 1] : "MID";
            string pLeftA = small > 0 ? posA[small - 1] : "STR";
            string pRightA = large < pos.Length - 1 ? posA[large + 1] : "END";
            string pLeftRightA = small < large - 1 ? posA[small + 1] : "MID";
            string pRightLeftA = large > small + 1 ? posA[large - 1] : "MID";

            // feature posR posMid posL
            for (int i = small + 1; i < large; i++)
            {
                string allPos = pos[small] + " " + pos[i] + " " + pos[large];
                string allPosA = posA[small] + " " + posA[i] + " " + posA[large];
                Add("PC=" + allPos + attDist, 1.0, fv);
                Add("1PC=" + allPos, 1.0, fv);
                Add("XPC=" + allPosA + attDist, 1.0, fv);
                Add("X1PC=" + allPosA, 1.0, fv);
            }

            // feature posL-1 posL posR posR+1
            Add("PT=" + pLeft + " " + pos[small] + " " + pos[large] + " " + pRight + attDist, 1.0, fv);
            Add("PT1=" + pos[small] + " " + pos[large] + " " + pRight + attDist, 1.0, fv);
            Add("PT2=" + pLeft + " " + pos[small] + " " + pos[large] + attDist, 1.0, fv);
            Add("PT3=" + pLeft + " " + pos[large] + " " + pRight + attDist, 1.0, fv);
            Add("PT4=" + pLeft + " " + pos[small] + " " + pRight + attDist, 1.0, fv);

            Add("1PT=" + pLeft + " " + pos[small] + " " + pos[large] + " " + pRight, 1.0, fv);
            Add("1PT1=" + pos[small] + " " + pos[large] + " " + pRight, 1.0, fv);
            Add("1PT2=" + pLeft + " " + pos[small] + " " + pos[large], 1.0, fv);
            Add("1PT3=" + pLeft + " " + pos[large] + " " + pRight, 1.0, fv);
            Add("1PT4=" + pLeft + " " + pos[small] + " " + pRight, 1.0, fv);

            Add("XPT=" + pLeftA + " " + posA[small] + " " + posA[large] + " " + pRightA + attDist, 1.0, fv);
            Add("XPT1=" + posA[small] + " " + posA[large] + " " + pRightA + attDist, 1.0, fv);
            Add("XPT2=" + pLeftA + " " + posA[small] + " " + posA[large] + attDist, 1.0, fv);
            Add("XPT3=" + pLeftA + " " + posA[large] + " " + pRightA + attDist, 1.0, fv);
            Add("XPT4=" + pLeftA + " " + posA[small] + " " + pRightA + attDist, 1.0, fv);

            Add("X1PT=" + pLeftA + " " + posA[small] + " " + posA[large] + " " + pRightA, 1.0, fv);
            Add("X1PT1=" + posA[small] + " " + posA[large] + " " + pRightA, 1.0, fv);
            Add("X1PT2=" + pLeftA + " " + posA[small] + " " + posA[large], 1.0, fv);
            Add("X1PT3=" + pLeftA + " " + posA[large] + " " + pRightA, 1.0, fv);
            Add("X1PT4=" + pLeftA + " " + posA[small] + " " + pRightA, 1.0, fv);

            // feature posL posL+1 posR-1 posR
            Add("APT=" + pos[small] + " " + pLeftRight + " "
                + pRightLeft + " " + pos[large] + attDist, 1.0, fv);
            Add("APT1=" + pos[small] + " " + pRightLeft + " " + pos[large] + attDist, 1.0, fv);
            Add("APT2=" + pos[small] + " " + pLeftRight + " " + pos[large] + attDist, 1.0, fv);
            Add("APT3=" + pLeftRight + " " + pRightLeft + " " + pos[large] + attDist, 1.0, fv);
            Add("APT4=" + pos[small] + " " + pLeftRight + " " + pRightLeft + attDist, 1.0, fv);

            Add("1APT=" + pos[small] + " " + pLeftRight + " "
                + pRightLeft + " " + pos[large], 1.0, fv);
            Add("1APT1=" + pos[small] + " " + pRightLeft + " " + pos[large], 1.0, fv);
            Add("1APT2=" + pos[small] + " " + pLeftRight + " " + pos[large], 1.0, fv);
            Add("1APT3=" + pLeftRight + " " + pRightLeft + " " + pos[large], 1.0, fv);
            Add("1APT4=" + pos[small] + " " + pLeftRight + " " + pRightLeft, 1.0, fv);

            Add("XAPT=" + posA[small] + " " + pLeftRightA + " "
                + pRightLeftA + " " + posA[large] + attDist, 1.0, fv);
            Add("XAPT1=" + posA[small] + " " + pRightLeftA + " " + posA[large] + attDist, 1.0, fv);
            Add("XAPT2=" + posA[small] + " " + pLeftRightA + " " + posA[large] + attDist, 1.0, fv);
            Add("XAPT3=" + pLeftRightA + " " + pRightLeftA + " " + posA[large] + attDist, 1.0, fv);
            Add("XAPT4=" + posA[small] + " " + pLeftRightA + " " + pRightLeftA + attDist, 1.0, fv);

            Add("X1APT=" + posA[small] + " " + pLeftRightA + " "
                + pRightLeftA + " " + posA[large], 1.0, fv);
            Add("X1APT1=" + posA[small] + " " + pRightLeftA + " " + posA[large], 1.0, fv);
            Add("X1APT2=" + posA[small] + " " + pLeftRightA + " " + posA[large], 1.0, fv);
            Add("X1APT3=" + pLeftRightA + " " + pRightLeftA + " " + posA[large], 1.0, fv);
            Add("X1APT4=" + posA[small] + " " + pLeftRightA + " " + pRightLeftA, 1.0, fv);

            // feature posL-1 posL posR-1 posR
            // feature posL posL+1 posR posR+1
            Add("BPT=" + pLeft + " " + pos[small] + " " + pRightLeft + " " + pos[large] + attDist, 1.0, fv);
            Add("1BPT=" + pLeft + " " + pos[small] + " " + pRightLeft + " " + pos[large], 1.0, fv);
            Add("CPT=" + pos[small] + " " + pLeftRight + " " + pos[large] + " " + pRight + attDist, 1.0, fv);
            Add("1CPT=" + pos[small] + " " + pLeftRight + " " + pos[large] + " " + pRight, 1.0, fv);

            Add("XBPT=" + pLeftA + " " + posA[small] + " " + pRightLeftA + " " + posA[large] + attDist, 1.0, fv);
            Add("X1BPT=" + pLeftA + " " + posA[small] + " " + pRightLeftA + " " + posA[large], 1.0, fv);
            Add("XCPT=" + posA[small] + " " + pLeftRightA + " " + posA[large] + " " + pRightA + attDist, 1.0, fv);
            Add("X1CPT=" + posA[small] + " " + pLeftRightA + " " + posA[large] + " " + pRightA, 1.0, fv);

            string head = attR ? toks[small] : toks[large];
            string headP = attR ? pos[small] : pos[large];
            string child = attR ? toks[large] : toks[small];
            string childP = attR ? pos[large] : pos[small];

            string all = head + " " + headP + " " + child + " " + childP;
            string hPos = headP + " " + child + " " + childP;
            string cPos = head + " " + headP + " " + childP;
            string hP = headP + " " + child;
            string cP = head + " " + childP;
            string oPos = headP + " " + childP;
            string oLex = head + " " + child;

            Add("A=" + all + attDist, 1.0, fv); //this
            Add("B=" + hPos + attDist, 1.0, fv);
            Add("C=" + cPos + attDist, 1.0, fv);
            Add("D=" + hP + attDist, 1.0, fv);
            Add("E=" + cP + attDist, 1.0, fv);
            Add("F=" + oLex + attDist, 1.0, fv); //this
            Add("G=" + oPos + attDist, 1.0, fv);
            Add("H=" + head + " " + headP + attDist, 1.0, fv);
            Add("I=" + headP + attDist, 1.0, fv);
            Add("J=" + head + attDist, 1.0, fv); //this
            Add("K=" + child + " " + childP + attDist, 1.0, fv);
            Add("L=" + childP + attDist, 1.0, fv);
            Add("M=" + child + attDist, 1.0, fv); //this

            Add("AA=" + all, 1.0, fv); //this
            Add("BB=" + hPos, 1.0, fv);
            Add("CC=" + cPos, 1.0, fv);
            Add("DD=" + hP, 1.0, fv);
            Add("EE=" + cP, 1.0, fv);
            Add("FF=" + oLex, 1.0, fv); //this
            Add("GG=" + oPos, 1.0, fv);
            Add("HH=" + head + " " + headP, 1.0, fv);
            Add("II=" + headP, 1.0, fv);
            Add("JJ=" + head, 1.0, fv); //this
            Add("KK=" + child + " " + childP, 1.0, fv);
            Add("LL=" + childP, 1.0, fv);
            Add("MM=" + child, 1.0, fv); //this

            if (head.Length > 5 || child.Length > 5)
            {
                int hL = head.Length;
                int cL = child.Length;

                head = hL > 5 ? head.SubstringWithIndex(0, 5) : head;
                child = cL > 5 ? child.SubstringWithIndex(0, 5) : child;

                all = head + " " + headP + " " + child + " " + childP;
                hPos = headP + " " + child + " " + childP;
                cPos = head + " " + headP + " " + childP;
                hP = headP + " " + child;
                cP = head + " " + childP;
                oPos = headP + " " + childP;
                oLex = head + " " + child;

                Add("SA=" + all + attDist, 1.0, fv); //this
                Add("SF=" + oLex + attDist, 1.0, fv); //this
                Add("SAA=" + all, 1.0, fv); //this
                Add("SFF=" + oLex, 1.0, fv); //this

                if (cL > 5)
                {
                    Add("SB=" + hPos + attDist, 1.0, fv);
                    Add("SD=" + hP + attDist, 1.0, fv);
                    Add("SK=" + child + " " + childP + attDist, 1.0, fv);
                    Add("SM=" + child + attDist, 1.0, fv); //this
                    Add("SBB=" + hPos, 1.0, fv);
                    Add("SDD=" + hP, 1.0, fv);
                    Add("SKK=" + child + " " + childP, 1.0, fv);
                    Add("SMM=" + child, 1.0, fv); //this
                }
                if (hL > 5)
                {
                    Add("SC=" + cPos + attDist, 1.0, fv);
                    Add("SE=" + cP + attDist, 1.0, fv);
                    Add("SH=" + head + " " + headP + attDist, 1.0, fv);
                    Add("SJ=" + head + attDist, 1.0, fv); //this

                    Add("SCC=" + cPos, 1.0, fv);
                    Add("SEE=" + cP, 1.0, fv);
                    Add("SHH=" + head + " " + headP, 1.0, fv);
                    Add("SJJ=" + head, 1.0, fv); //this
                }
            }

            return fv;
        }

        public virtual FeatureVector CreateFeatureVector(string[] toks,
                                                 string[] pos,
                                                 string[] posA,
                                                 int word,
                                                 string type,
                                                 bool attR,
                                                 bool childFeatures,
                                                 FeatureVector fv)
        {
            if (!Labeled) return fv;

            string att = "";
            if (attR)
                att = "RA";
            else
                att = "LA";

            att += "&" + childFeatures;

            string w = toks[word];
            string wP = pos[word];

            string wPm1 = word > 0 ? pos[word - 1] : "STR";
            string wPp1 = word < pos.Length - 1 ? pos[word + 1] : "END";

            Add("NTS1=" + type + "&" + att, 1.0, fv);
            Add("ANTS1=" + type, 1.0, fv);
            for (int i = 0; i < 2; i++)
            {
                string suff = i < 1 ? "&" + att : "";
                suff = "&" + type + suff;

                Add("NTH=" + w + " " + wP + suff, 1.0, fv);
                Add("NTI=" + wP + suff, 1.0, fv);
                Add("NTIA=" + wPm1 + " " + wP + suff, 1.0, fv);
                Add("NTIB=" + wP + " " + wPp1 + suff, 1.0, fv);
                Add("NTIC=" + wPm1 + " " + wP + " " + wPp1 + suff, 1.0, fv);
                Add("NTJ=" + w + suff, 1.0, fv); //this
            }

            return fv;
        }

        public virtual FeatureVector CreateFeatureVector(string[] toks,
                                                 string[] pos,
                                                 string[] labs,
                                                 int[] deps)
        {
            var posA = new string[pos.Length];
            for (int i = 0; i < pos.Length; i++)
            {
                posA[i] = pos[i].SubstringWithIndex(0, 1);
            }

            var fv = new FeatureVector();
            for (int i = 0; i < toks.Length; i++)
            {
                if (deps[i] == -1)
                    continue;
                int small = i < deps[i] ? i : deps[i];
                int large = i > deps[i] ? i : deps[i];
                bool attR = i < deps[i] ? false : true;
                fv = CreateFeatureVector(toks, pos, posA, small, large, attR, fv);
                if (Labeled)
                {
                    fv = CreateFeatureVector(toks, pos, posA, i, labs[i], attR, true, fv);
                    fv = CreateFeatureVector(toks, pos, posA, deps[i], labs[i], attR, false, fv);
                }
            }
            return fv;
        }

        public void Add(string feat, double val,  FeatureVector fv)
        {
            int num = DataAlphabet.LookupIndex(feat);
            if (num >= 0)
                fv.FVector.AddFirst(new Feature(num, val));
        }

        public void AddNewFeature(int index, double val, FeatureVector fv)
        {
            fv.FVector.AddFirst(new Feature(index, val));
        }

        public virtual void WritePossibleFeatures(DependencyInstance inst, BinaryWriter writer)
        {
            var toks = inst.Sentence;
            var pos = inst.POS;
            var labs = inst.Labs;

            var posA = new string[pos.Length];
            for (int i = 0; i < pos.Length; i++)
            {
                posA[i] = pos[i].SubstringWithIndex(0, 1);
            }


            try
            {
                for (int w1 = 0; w1 < toks.Length; w1++)
                {
                    for (int w2 = w1 + 1; w2 < toks.Length; w2++)
                    {
                        for (int ph = 0; ph < 2; ph++)
                        {
                            bool attR = ph == 0 ? true : false;

                            var childInt = attR ? w2 : w1;
                            var parInt = attR ? w1 : w2;

                            var prodFV = CreateFeatureVector(toks, pos, posA, w1, w2, attR,
                                                                       new FeatureVector());

                            foreach (Feature feature in prodFV.FVector)
                            {
                                if (feature.Index >= 0)
                                    writer.Write(feature.Index);
                            }
                            writer.Write(-2);
                        }
                    }
                }

                writer.Write(-3);

                if (Labeled)
                {
                    for (int w1 = 0; w1 < toks.Length; w1++)
                    {
                        for (int t = 0; t < Types.Length; t++)
                        {
                            string type = Types[t];

                            for (int ph = 0; ph < 2; ph++)
                            {
                                bool attR = ph == 0 ? true : false;

                                for (int ch = 0; ch < 2; ch++)
                                {
                                    bool child = ch == 0 ? true : false;

                                    var prodFV = CreateFeatureVector(toks, pos, posA, w1,
                                                                               type,
                                                                               attR, child,
                                                                               new FeatureVector());
                                    foreach (Feature feature in prodFV.FVector)
                                    {
                                        if (feature.Index >= 0)
                                            writer.Write(feature.Index);
                                    }
                                    writer.Write(-2);
                                }
                            }
                        }
                    }

                    writer.Write(-3);
                }
                foreach (Feature feature in inst.Fv.FVector)
                {
                    writer.Write(feature.Index);
                }

                writer.Write(-4);
                writer.Write(inst.Sentence.Length);
                foreach (string s in inst.Sentence)
                {
                    writer.Write(s);
                }
                writer.Write(-5);
                writer.Write(inst.POS.Length); 
                foreach (string s in inst.POS)
                {
                    writer.Write(s);
                }
                writer.Write(-6);
                writer.Write(inst.Labs.Length);
                foreach (string s in inst.Labs)
                {
                    writer.Write(s);
                }
                writer.Write(-7);
                writer.Write(inst.ActParseTree);
                writer.Write(-1);
            }
            catch (IOException)
            {
            }
        }

        public DependencyInstance ReadFeatureVector(BinaryReader reader,
                                                   DependencyInstance inst,
                                                   FeatureVector[,,] fvs,
                                                   double[,,] probs,
                                                   FeatureVector[,,,] ntFvs,
                                                   double[,,,] ntProbs,
                                                   Parameters parameters)
        {
            int length = inst.Length;

            // Get production crap.		
            for (int w1 = 0; w1 < length; w1++)
            {
                for (int w2 = w1 + 1; w2 < length; w2++)
                {
                    for (int ph = 0; ph < 2; ph++)
                    {
                        var prodFV = new FeatureVector();

                        int indx = reader.ReadInt32();
                        while (indx != -2)
                        {
                            AddNewFeature(indx, 1.0, prodFV);
                            indx = reader.ReadInt32();
                        }

                        double prodProb = parameters.GetScore(prodFV);
                        fvs[w1, w2, ph] = prodFV;
                        probs[w1, w2, ph] = prodProb;
                    }
                }
            }
            int last = reader.ReadInt32();
            if (last != -3)
            {
                Console.WriteLine("Error reading file.");

                throw new Exception("Bad File Format");
            }

            if (Labeled)
            {
                for (int w1 = 0; w1 < length; w1++)
                {
                    for (int t = 0; t < Types.Length; t++)
                    {
                        string type = Types[t];

                        for (int ph = 0; ph < 2; ph++)
                        {
                            for (int ch = 0; ch < 2; ch++)
                            {
                                var prodFV = new FeatureVector();

                                int indx = reader.ReadInt32();
                                while (indx != -2)
                                {
                                    AddNewFeature(indx, 1.0, prodFV);
                                    indx = reader.ReadInt32();
                                }

                                double ntProb = parameters.GetScore(prodFV);
                                ntFvs[w1, t, ph, ch] = prodFV;
                                ntProbs[w1, t, ph, ch] = ntProb;
                            }
                        }
                    }
                }
                last = reader.ReadInt32();
                if (last != -3)
                {
                    Console.WriteLine("Error reading file.");
                    throw new Exception("Bad File Format");
                }
            }

            var nfv = new FeatureVector();
            int next = reader.ReadInt32();
            while (next != -4)
            {
                AddNewFeature(next, 1.0, nfv);
                next = reader.ReadInt32();
            }

            string[] toks = null;
            string[] pos = null;
            string[] labs = null;
            string actParseTree = null;
            try
            {
                int len = reader.ReadInt32();
                toks = new string[len];
                for (int i = 0; i < len; i++)
                {
                    toks[i] = reader.ReadString();
                }
                next = reader.ReadInt32();
                len = reader.ReadInt32();
                pos = new string[len];
                for (int i = 0; i < len; i++)
                {
                    pos[i] = reader.ReadString();
                }
                next = reader.ReadInt32();
                len = reader.ReadInt32();
                labs = new string[len];
                for (int i = 0; i < len; i++)
                {
                    labs[i] = reader.ReadString();
                }
                next = reader.ReadInt32();
                actParseTree = reader.ReadString();
                next = reader.ReadInt32();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error reading file.");
                throw new Exception("Bad File Format");
            }

            if (next != -1)
            {
                Console.WriteLine("Error reading file.");
                throw new Exception("Bad File Format");
            }

            var pti = new DependencyInstance(toks, pos, labs, nfv);
            pti.ActParseTree = actParseTree;
            return pti;
        }

        public void GetFeatureVector(DependencyInstance inst,
                                     FeatureVector[,,] fvs,
                                     double[,,] probs,
                                     FeatureVector[,,,] ntFvs,
                                     double[,,,] ntProbs, Parameters parameters)
        {
            string[] toks = inst.Sentence;
            string[] pos = inst.POS;
            string[] labs = inst.Labs;

            var posA = new string[pos.Length];
            for (int i = 0; i < pos.Length; i++)
            {
                posA[i] = pos[i].SubstringWithIndex(0, 1);
            }

            // Get production crap.		
            for (int w1 = 0; w1 < toks.Length; w1++)
            {
                for (int w2 = w1 + 1; w2 < toks.Length; w2++)
                {
                    for (int ph = 0; ph < 2; ph++)
                    {
                        bool attR = ph == 0 ? true : false;

                        int childInt = attR ? w2 : w1;
                        int parInt = attR ? w1 : w2;

                        FeatureVector prodFV = CreateFeatureVector(toks, pos, posA, w1, w2, attR,
                                                                   new FeatureVector());

                        double prodProb = parameters.GetScore(prodFV);
                        fvs[w1, w2, ph] = prodFV;
                        probs[w1, w2, ph] = prodProb;
                    }
                }
            }

            if (Labeled)
            {
                for (int w1 = 0; w1 < toks.Length; w1++)
                {
                    for (int t = 0; t < Types.Length; t++)
                    {
                        string type = Types[t];

                        for (int ph = 0; ph < 2; ph++)
                        {
                            bool attR = ph == 0 ? true : false;

                            for (int ch = 0; ch < 2; ch++)
                            {
                                bool child = ch == 0 ? true : false;

                                FeatureVector prodFV = CreateFeatureVector(toks, pos, posA, w1,
                                                                           type, attR, child,
                                                                           new FeatureVector());

                                double ntProb = parameters.GetScore(prodFV);
                                ntFvs[w1, t, ph, ch] = prodFV;
                                ntProbs[w1, t, ph, ch] = ntProb;
                            }
                        }
                    }
                }
            }
        }
    }
}