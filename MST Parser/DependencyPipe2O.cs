using System;
using System.IO;
using MSTParser;
using MSTParser.Extensions;

namespace MSTParser
{
    public class DependencyPipe2O : DependencyPipe
    {
        public DependencyPipe2O()
        {
        }

        public DependencyPipe2O(bool createForest)
            : base(createForest)
        {
        }

        public FeatureVector CreateFeatureVector(string[] toks,
                                                 string[] pos,
                                                 string[] posA,
                                                 int par,
                                                 int ch1, int ch2,
                                                 FeatureVector fv)
        {
            // ch1 is always the closes to par
            string dir = par > ch2 ? "RA" : "LA";

            string parPOS = pos[par];
            string ch1POS = ch1 == par ? "STPOS" : pos[ch1];
            string ch2POS = pos[ch2];
            string ch1Word = ch1 == par ? "STWRD" : toks[ch1];
            string ch2Word = toks[ch2];

            string pTrip = parPOS + "_" + ch1POS + "_" + ch2POS;
            Add("POS_TRIP=" + pTrip + "_" + dir, 1.0, fv);
            Add("APOS_TRIP=" + pTrip, 1.0, fv);

            return fv;
        }

        public FeatureVector CreateFeatureVectorSib(string[] toks,
                                                    string[] pos,
                                                    int ch1, int ch2,
                                                    bool isST,
                                                    FeatureVector fv)
        {
            // ch1 is always the closes to par
            string dir = ch1 > ch2 ? "RA" : "LA";

            string ch1POS = isST ? "STPOS" : pos[ch1];
            string ch2POS = pos[ch2];
            string ch1Word = isST ? "STWRD" : toks[ch1];
            string ch2Word = toks[ch2];

            Add("CH_PAIR=" + ch1POS + "_" + ch2POS + "_" + dir, 1.0, fv);
            Add("CH_WPAIR=" + ch1Word + "_" + ch2Word + "_" + dir, 1.0, fv);
            Add("CH_WPAIRA=" + ch1Word + "_" + ch2POS + "_" + dir, 1.0, fv);
            Add("CH_WPAIRB=" + ch1POS + "_" + ch2Word + "_" + dir, 1.0, fv);
            Add("ACH_PAIR=" + ch1POS + "_" + ch2POS, 1.0, fv);
            Add("ACH_WPAIR=" + ch1Word + "_" + ch2Word, 1.0, fv);
            Add("ACH_WPAIRA=" + ch1Word + "_" + ch2POS, 1.0, fv);
            Add("ACH_WPAIRB=" + ch1POS + "_" + ch2Word, 1.0, fv);

            int dist = Math.Max(ch1, ch2) - Math.Min(ch1, ch2);
            string distBool = "0";
            if (dist > 1)
                distBool = "1";
            if (dist > 2)
                distBool = "2";
            if (dist > 3)
                distBool = "3";
            if (dist > 4)
                distBool = "4";
            if (dist > 5)
                distBool = "5";
            if (dist > 10)
                distBool = "10";
            Add("SIB_PAIR_DIST=" + distBool + "_" + dir, 1.0, fv);
            Add("ASIB_PAIR_DIST=" + distBool, 1.0, fv);
            Add("CH_PAIR_DIST=" + ch1POS + "_" + ch2POS + "_" + distBool + "_" + dir, 1.0, fv);
            Add("ACH_PAIR_DIST=" + ch1POS + "_" + ch2POS + "_" + distBool, 1.0, fv);


            return fv;
        }

        public FeatureVector CreateFeatureVector(string[] toks,
                                                 string[] pos, int[] labs1,
                                                 int[] deps)
        {
            var labs = new string[labs1.Length];
            for (int i = 0; i < labs.Length; i++)
                labs[i] = Types[labs1[i]];

            return CreateFeatureVector(toks, pos, labs, deps);
        }

        public override FeatureVector CreateFeatureVector(string[] toks,
                                                 string[] pos, string[] labs,
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
            // find all trip features
            for (int i = 0; i < toks.Length; i++)
            {
                if (deps[i] == -1 && i != 0) continue;
                // Right children
                int prev = i;
                for (int j = i + 1; j < toks.Length; j++)
                {
                    if (deps[j] == i)
                    {
                        fv = CreateFeatureVector(toks, pos, posA, i, prev, j, fv);
                        fv = CreateFeatureVectorSib(toks, pos, prev, j, prev == i, fv);
                        prev = j;
                    }
                }
                prev = i;
                for (int j = i - 1; j >= 0; j--)
                {
                    if (deps[j] == i)
                    {
                        fv = CreateFeatureVector(toks, pos, posA, i, prev, j, fv);
                        fv = CreateFeatureVectorSib(toks, pos, prev, j, prev == i, fv);
                        prev = j;
                    }
                }
            }

            return fv;
        }


        public override void WritePossibleFeatures(DependencyInstance inst, BinaryWriter out_)
        {
            string[] toks = inst.Sentence;
            string[] pos = inst.POS;
            string[] labs = inst.Labs;

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

                            FeatureVector prodFV = CreateFeatureVector(toks, pos, posA, w1, w2, attR,
                                                                       new FeatureVector());
                            foreach (Feature feature in prodFV.FVector)
                            {
                                if (feature.Index >= 0)
                                    out_.Write(feature.Index);
                            }
                            out_.Write(-2);
                        }
                    }
                }

                out_.Write(-3);

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
                                                                               type,
                                                                               attR, child,
                                                                               new FeatureVector());
                                    foreach (Feature feature in prodFV.FVector)
                                    {
                                        if (feature.Index >= 0)
                                            out_.Write(feature.Index);
                                    }

                                    out_.Write(-2);
                                }
                            }
                        }
                    }

                    out_.Write(-3);
                }

                for (int w1 = 0; w1 < toks.Length; w1++)
                {
                    for (int w2 = w1; w2 < toks.Length; w2++)
                    {
                        for (int w3 = w2 + 1; w3 < toks.Length; w3++)
                        {
                            FeatureVector prodFV = CreateFeatureVector(toks, pos, posA, w1, w2, w3,
                                                                       new FeatureVector());
                            foreach (Feature feature in prodFV.FVector)
                            {
                                if (feature.Index >= 0)
                                    out_.Write(feature.Index);
                            }

                            out_.Write(-2);
                        }
                    }
                    for (int w2 = w1; w2 >= 0; w2--)
                    {
                        for (int w3 = w2 - 1; w3 >= 0; w3--)
                        {
                            FeatureVector prodFV = CreateFeatureVector(toks, pos, posA, w1, w2, w3,
                                                                       new FeatureVector());
                            foreach (Feature feature in prodFV.FVector)
                            {
                                if (feature.Index >= 0)
                                    out_.Write(feature.Index);
                            }
                            out_.Write(-2);
                        }
                    }
                }

                out_.Write(-3);

                for (int w1 = 0; w1 < toks.Length; w1++)
                {
                    for (int w2 = 0; w2 < toks.Length; w2++)
                    {
                        for (int wh = 0; wh < 2; wh++)
                        {
                            if (w1 != w2)
                            {
                                FeatureVector prodFV = CreateFeatureVectorSib(toks, pos, w1, w2, wh == 0,
                                                                              new FeatureVector());
                                foreach (Feature feature in prodFV.FVector)
                                {
                                    if (feature.Index >= 0)
                                        out_.Write(feature.Index);
                                }
                                out_.Write(-2);
                            }
                        }
                    }
                }

                out_.Write(-3);

                foreach (Feature feature in inst.Fv.FVector)
                {
                    out_.Write(feature.Index);
                }

                out_.Write(-4);
                out_.Write(inst.Sentence.Length); 
                foreach (string s in inst.Sentence)
                {
                    out_.Write(s);
                }
                out_.Write(inst.POS.Length); 
                foreach (string s in inst.POS)
                {
                    out_.Write(s);
                }
                out_.Write(-6);
                out_.Write(inst.Labs.Length);
                foreach (string s in inst.Labs)
                {
                    out_.Write(s);
                }
                out_.Write(-7);
                out_.Write(inst.ActParseTree);

                out_.Write(-1);
            }
            catch (IOException)
            {
            }
        }

        // TODO: sina: rename it to ReadFeatureVector
        public DependencyInstance GetFeatureVector(BinaryReader reader,
                                                   DependencyInstance inst,
                                                   FeatureVector[,,] fvs,
                                                   double[,,] probs,
                                                   FeatureVector[,,] fvsTrips,
                                                   double[,,] probsTrips,
                                                   FeatureVector[,,] fvsSibs,
                                                   double[,,] probsSibs,
                                                   FeatureVector[,,,] ntFvs,
                                                   double[,,,] ntProbs,
                                                   Parameters @params)
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

                        double prodProb = @params.GetScore(prodFV);
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

                                double ntProb = @params.GetScore(prodFV);
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

            for (int w1 = 0; w1 < length; w1++)
            {
                for (int w2 = w1; w2 < length; w2++)
                {
                    for (int w3 = w2 + 1; w3 < length; w3++)
                    {
                        var prodFV = new FeatureVector();

                        int indx = reader.ReadInt32();
                        while (indx != -2)
                        {
                            AddNewFeature(indx, 1.0, prodFV);
                            indx = reader.ReadInt32();
                        }

                        double prodProb = @params.GetScore(prodFV);
                        fvsTrips[w1, w2, w3] = prodFV;
                        probsTrips[w1, w2, w3] = prodProb;
                    }
                }
                for (int w2 = w1; w2 >= 0; w2--)
                {
                    for (int w3 = w2 - 1; w3 >= 0; w3--)
                    {
                        var prodFV = new FeatureVector();

                        int indx = reader.ReadInt32();
                        while (indx != -2)
                        {
                            AddNewFeature(indx, 1.0, prodFV);

                            indx = reader.ReadInt32();
                        }

                        double prodProb = @params.GetScore(prodFV);
                        fvsTrips[w1, w2, w3] = prodFV;
                        probsTrips[w1, w2, w3] = prodProb;
                    }
                }
            }

            last = reader.ReadInt32();
            if (last != -3)
            {
                Console.WriteLine("Error reading file.");
                throw new Exception("Bad File Format");
            }

            for (int w1 = 0; w1 < length; w1++)
            {
                for (int w2 = 0; w2 < length; w2++)
                {
                    for (int wh = 0; wh < 2; wh++)
                    {
                        if (w1 != w2)
                        {
                            var prodFV = new FeatureVector();

                            int indx = reader.ReadInt32();
                            while (indx != -2)
                            {
                                AddNewFeature(indx, 1.0, prodFV);
                                indx = reader.ReadInt32();
                            }

                            double prodProb = @params.GetScore(prodFV);
                            fvsSibs[w1, w2, wh] = prodFV;
                            probsSibs[w1, w2, wh] = prodProb;
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
                int len = reader.ReadInt32(); //Added by MSR
                toks = new string[len];
                for (int i = 0; i < len; i++)
                {
                    toks[i] = reader.ReadString();
                }
                next = reader.ReadInt32();
                len = reader.ReadInt32(); //Added by MSR
                pos = new string[len];
                for (int i = 0; i < len; i++)
                {
                    pos[i] = reader.ReadString();
                }
                next = reader.ReadInt32();
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
                // TODO: sina: A library MUST NOT call Environment.Exit in any form
                // throw exception instead.
                Console.WriteLine("Error reading file.");
                throw new Exception("Bad File Format");
            }

            if (next != -1)
            {
                // TODO: sina: A library MUST NOT call Environment.Exit in any form
                // throw exception instead.
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
                                     FeatureVector[,,] fvsTrips,
                                     double[,,] probsTrips,
                                     FeatureVector[,,] fvsSibs,
                                     double[,,] probsSibs,
                                     FeatureVector[,,,] ntFvs,
                                     double[,,,] ntProbs, Parameters @params)
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

                        double prodProb = @params.GetScore(prodFV);
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

                                double ntProb = @params.GetScore(prodFV);
                                ntFvs[w1, t, ph, ch] = prodFV;
                                ntProbs[w1, t, ph, ch] = ntProb;
                            }
                        }
                    }
                }
            }


            for (int w1 = 0; w1 < toks.Length; w1++)
            {
                for (int w2 = w1; w2 < toks.Length; w2++)
                {
                    for (int w3 = w2 + 1; w3 < toks.Length; w3++)
                    {
                        FeatureVector prodFV = CreateFeatureVector(toks, pos, posA, w1, w2, w3,
                                                                   new FeatureVector());
                        double prodProb = @params.GetScore(prodFV);
                        fvsTrips[w1, w2, w3] = prodFV;
                        probsTrips[w1, w2, w3] = prodProb;
                    }
                }
                for (int w2 = w1; w2 >= 0; w2--)
                {
                    for (int w3 = w2 - 1; w3 >= 0; w3--)
                    {
                        FeatureVector prodFV = CreateFeatureVector(toks, pos, posA, w1, w2, w3,
                                                                   new FeatureVector());
                        double prodProb = @params.GetScore(prodFV);
                        fvsTrips[w1, w2, w3] = prodFV;
                        probsTrips[w1, w2, w3] = prodProb;
                    }
                }
            }

            for (int w1 = 0; w1 < toks.Length; w1++)
            {
                for (int w2 = 0; w2 < toks.Length; w2++)
                {
                    for (int wh = 0; wh < 2; wh++)
                    {
                        if (w1 != w2)
                        {
                            FeatureVector prodFV = CreateFeatureVectorSib(toks, pos, w1, w2, wh == 0,
                                                                          new FeatureVector());
                            double prodProb = @params.GetScore(prodFV);
                            fvsSibs[w1, w2, wh] = prodFV;
                            probsSibs[w1, w2, wh] = prodProb;
                        }
                    }
                }
            }
        }
    }
}