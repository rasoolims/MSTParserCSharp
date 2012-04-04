using System;
using System.IO;
using System.Text;
using MSTParser;

namespace MSTParser
{
    public class DependencyParser
    {
        public static ProjectiveTypes DecodeType = ProjectiveTypes.Projective;

        public static LossTypes LossType = LossTypes.Punc;

        public static string DefaultModelFileName = "dep.model";

        public static string DefaultOutFile = "out.txt";

        public bool CreateForest = true;

        public static bool Eval = false;
        public static string Goldfile = null;

        public static bool MTrain = false;
        public static int NumIters = 1;

        public static bool SecondOrder = false;
        public static bool Test = false;
        public static string TestfileName = null;
        public static string TestforestName = null;
        public static int TestK = 1;
        public static string TrainfileName = null;
        public static string TrainforestName = null;
        public static int TrainK = 1;


        private readonly DependencyDecoder m_decoder;
        private readonly Parameters m_params;
        private readonly DependencyPipe m_pipe;

        public DependencyParser(int order, string modelFileName, bool createForest)
        {
            DependencyPipe pipe = order == 2 ? new DependencyPipe2O(true) : new DependencyPipe(true);
            pipe.setLabel(createForest);
            m_pipe = pipe;
            m_params = new Parameters(pipe.DataAlphabet.Count);
            m_decoder = SecondOrder ? new DependencyDecoder2O(pipe) : new DependencyDecoder(pipe);
            LoadModel(modelFileName);
            pipe.CloseAlphabets();
        }


        public DependencyParser(DependencyPipe pipe)
        {
            m_pipe = pipe;
            m_params = new Parameters(pipe.DataAlphabet.Count);
            m_decoder = SecondOrder ? new DependencyDecoder2O(pipe) : new DependencyDecoder(pipe);
        }


        public void Train(DependencyInstance[] il, string trainfile, string trainForest)
        {
            Console.WriteLine("About to Train");
            Console.WriteLine("Num Feats: " + m_pipe.DataAlphabet.Count);

            int i = 0;
            for (i = 0; i < NumIters; i++)
            {
                Console.WriteLine("========================");
                Console.WriteLine("Iteration: " + i);
                Console.WriteLine("========================");
                Console.Write("Processed: ");

                long start = DateTime.Now.Ticks*10000;

                TrainingIter(il, trainfile, trainForest, i + 1);

                long end = DateTime.Now.Ticks*10000;
                Console.WriteLine("Training iter took: " + (end - start));
            }

            m_params.AverageParams(i*il.Length);
        }

        private void TrainingIter(DependencyInstance[] il, string trainfile, string train_forest, int iter)
        {
            int numUpd = 0;
            var in_ = new BinaryReader(new FileStream(train_forest, FileMode.Open));
            bool evaluateI = true;

            for (int i = 0; i < il.Length; i++)
            {
                if ((i + 1)%100 == 0)
                    Console.WriteLine("  " + (i + 1) + " instances");

                DependencyInstance inst = il[i];

                int length = inst.Length;

                // Get production crap.
                var fvs = new FeatureVector[length,length,2];
                var probs = new double[length,length,2];
                var ntFvs = new FeatureVector[length,m_pipe.Types.Length,2,2];
                var ntProbs = new double[length,m_pipe.Types.Length,2,2];
                var fvsTrips = new FeatureVector[length,length,length];
                var probsTrips = new double[length,length,length];
                var fvsSibs = new FeatureVector[length,length,2];
                var probsSibs = new double[length,length,2];

                if (SecondOrder)
                    inst = ((DependencyPipe2O) m_pipe).GetFeatureVector(in_, inst, fvs, probs,
                                                                      fvsTrips, probsTrips,
                                                                      fvsSibs, probsSibs,
                                                                      ntFvs, ntProbs, m_params);
                else
                    inst = m_pipe.ReadFeatureVector(in_, inst, fvs, probs, ntFvs, ntProbs, m_params);

                var upd = (double) (NumIters*il.Length - (il.Length*(iter - 1) + (i + 1)) + 1);
                int K = TrainK;
                object[,] d = null;
                if (DecodeType==ProjectiveTypes.Projective)
                {
                    if (SecondOrder)
                        d = ((DependencyDecoder2O) m_decoder).DecodeProjective(inst, fvs, probs,
                                                                             fvsTrips, probsTrips,
                                                                             fvsSibs, probsSibs,
                                                                             ntFvs, ntProbs, K);
                    else
                        d = m_decoder.DecodeProjective(inst, fvs, probs, ntFvs, ntProbs, K);
                }
                if (DecodeType==ProjectiveTypes.NonProjective)
                {
                    if (SecondOrder)
                        d = ((DependencyDecoder2O) m_decoder).DecodeNonProjective(inst, fvs, probs,
                                                                                fvsTrips, probsTrips,
                                                                                fvsSibs, probsSibs,
                                                                                ntFvs, ntProbs, K);
                    else
                        d = m_decoder.decodeNonProjective(inst, fvs, probs, ntFvs, ntProbs, K);
                }
                m_params.UpdateParamsMIRA(inst, d, upd);
            }
            Console.WriteLine("");

            Console.WriteLine("  " + il.Length + " instances");

            in_.Close();
        }

        ///////////////////////////////////////////////////////
        // Saving and loading models
        ///////////////////////////////////////////////////////
        public void SaveModel(string file)
        {
            var @out = new BinaryWriter(new FileStream(file, FileMode.Create));
            @out.Write(m_params.parameters.Length); 
            for (int i = 0; i < m_params.parameters.Length; i++)
            {
                @out.Write(m_params.parameters[i]);
            }
            m_pipe.DataAlphabet.WriteToStream(@out); 
            m_pipe.TypeAlphabet.WriteToStream(@out); 
            @out.Close();
        }

        public void LoadModel(string file)
        {
            var breader = new BinaryReader(new FileStream(file, FileMode.Open));
            int len = breader.ReadInt32();
            m_params.parameters = new double[len];
            for (int i = 0; i < len; i++)
            {
                m_params.parameters[i] = breader.ReadDouble();
            }
            m_pipe.DataAlphabet=Alphabet.ReadFromStream(breader);
            m_pipe.TypeAlphabet=Alphabet.ReadFromStream(breader);
            breader.Close();
            m_pipe.CloseAlphabets();
        }

        //////////////////////////////////////////////////////
        // Get Best Parses ///////////////////////////////////
        //////////////////////////////////////////////////////
        public void OutputParses(string tFile, string file)
        {
            long start = DateTime.Now.Ticks*10000;

            var pred = new StreamWriter(new FileStream(file, FileMode.Create), Encoding.UTF8);

            var in_ =
                new StreamReader(new FileStream(tFile, FileMode.Open), Encoding.UTF8);
            Console.Write("Processing Sentence: ");
            DependencyInstance il = m_pipe.CreateInstance(in_);
            int cnt = 0;
            while (il != null)
            {
                cnt++;
                Console.Write(cnt + " ");
                string[] toks = il.Sentence;

                int length = toks.Length;

                var fvs = new FeatureVector[toks.Length,toks.Length,2];
                var probs = new double[toks.Length,toks.Length,2];
                var ntFvs = new FeatureVector[toks.Length,m_pipe.Types.Length,2,2];
                var ntProbs = new double[toks.Length,m_pipe.Types.Length,2,2];
                var fvsTrips = new FeatureVector[length,length,length];
                var probsTrips = new double[length,length,length];
                var fvsSibs = new FeatureVector[length,length,2];
                var probsSibs = new double[length,length,2];
                if (SecondOrder)
                    ((DependencyPipe2O) m_pipe).GetFeatureVector(il, fvs, probs,
                                                               fvsTrips, probsTrips,
                                                               fvsSibs, probsSibs,
                                                               ntFvs, ntProbs, m_params);
                else
                    m_pipe.GetFeatureVector(il, fvs, probs, ntFvs, ntProbs, m_params);

                int K = TestK;
                object[,] d = null;
                if (DecodeType==ProjectiveTypes.Projective)
                {
                    if (SecondOrder)
                        d = ((DependencyDecoder2O) m_decoder).DecodeProjective(il, fvs, probs,
                                                                             fvsTrips, probsTrips,
                                                                             fvsSibs, probsSibs,
                                                                             ntFvs, ntProbs, K);
                    else
                        d = m_decoder.DecodeProjective(il, fvs, probs, ntFvs, ntProbs, K);
                }
                if (DecodeType == ProjectiveTypes.NonProjective)
                {
                    if (SecondOrder)
                        d = ((DependencyDecoder2O) m_decoder).DecodeNonProjective(il, fvs, probs,
                                                                                fvsTrips, probsTrips,
                                                                                fvsSibs, probsSibs,
                                                                                ntFvs, ntProbs, K);
                    else
                        d = m_decoder.decodeNonProjective(il, fvs, probs, ntFvs, ntProbs, K);
                }

                string[] res = ((string) d[0, 1]).Split(' ');
                string[] sent = il.Sentence;
                string[] pos = il.POS;
                var line1 = new StringBuilder();
                var line2 = new StringBuilder();
                var line3 = new StringBuilder();
                var line4 = new StringBuilder();
                for (int j = 1; j < pos.Length; j++)
                {
                    string[] trip = res[j - 1].Split("[\\|:]".ToCharArray());
                    line1.Append(sent[j] + "\t");
                    line2.Append(pos[j] + "\t");
                    line4.Append(trip[0] + "\t");
                    line3.Append(m_pipe.Types[int.Parse(trip[2])] + "\t");
                }
                var line=new StringBuilder();
                line.Append(line1.ToString().Trim()+"\n");
                line.Append(line2.ToString().Trim() + "\n");
                if(m_pipe.Labeled)
                    line.Append(line3.ToString().Trim() + "\n");
                line.Append(line4.ToString().Trim() + "\n\n");
                pred.Write(line.ToString());
                il = m_pipe.CreateInstance(in_);
            }
            Console.WriteLine();

            pred.Close();
            in_.Close();

            long end = DateTime.Now.Ticks*10000;
            Console.WriteLine("Took: " + (end - start));
        }
        public void OutputParses(string[] words, string[] posTags, out string[] labels, out int[] deps)
        {
            DependencyInstance il = m_pipe.CreateInstance(ref words,ref posTags, out labels,out deps);
            string[] toks = il.Sentence;

            int length = toks.Length;

            var fvs = new FeatureVector[toks.Length,toks.Length,2];
            var probs = new double[toks.Length,toks.Length,2];
            var ntFvs = new FeatureVector[toks.Length,m_pipe.Types.Length,2,2];
            var ntProbs = new double[toks.Length,m_pipe.Types.Length,2,2];
            var fvsTrips = new FeatureVector[length,length,length];
            var probsTrips = new double[length,length,length];
            var fvsSibs = new FeatureVector[length,length,2];
            var probsSibs = new double[length,length,2];
            if (SecondOrder)
                ((DependencyPipe2O) m_pipe).GetFeatureVector(il, fvs, probs,
                                                             fvsTrips, probsTrips,
                                                             fvsSibs, probsSibs,
                                                             ntFvs, ntProbs, m_params);
            else
                m_pipe.GetFeatureVector(il, fvs, probs, ntFvs, ntProbs, m_params);

            int K = TestK;
            object[,] d = null;
            if (DecodeType == ProjectiveTypes.Projective)
            {
                if (SecondOrder)
                    d = ((DependencyDecoder2O) m_decoder).DecodeProjective(il, fvs, probs,
                                                                           fvsTrips, probsTrips,
                                                                           fvsSibs, probsSibs,
                                                                           ntFvs, ntProbs, K);
                else
                    d = m_decoder.DecodeProjective(il, fvs, probs, ntFvs, ntProbs, K);
            }
            if (DecodeType == ProjectiveTypes.NonProjective)
            {
                if (SecondOrder)
                    d = ((DependencyDecoder2O) m_decoder).DecodeNonProjective(il, fvs, probs,
                                                                              fvsTrips, probsTrips,
                                                                              fvsSibs, probsSibs,
                                                                              ntFvs, ntProbs, K);
                else
                    d = m_decoder.decodeNonProjective(il, fvs, probs, ntFvs, ntProbs, K);
            }

            string[] res = ((string) d[0, 1]).Split(' ');
            string[] pos = il.POS;
            for (int j = 1; j < pos.Length; j++)
            {
                string[] trip = res[j - 1].Split("[\\|:]".ToCharArray());
                deps[j] = int.Parse(trip[0]);
                labels[j] = m_pipe.Types[int.Parse(trip[2])];
            }
        }
    }
}