using System;
using System.Collections.Generic;
using System.IO;

namespace MSTParser
{
    public class DependencyEvaluator
    {

        public EvaluationResult EvaluationRes { get; private set; }
        public Dictionary<string, int> FalseNegativeDic { get; private set; }
        public Dictionary<string, int> FalsePositiveDic { get; private set; }
        public Dictionary<string, int> TrueNegativeDic { get; private set; }
        public Dictionary<string, int> TruePositiveDic { get; private set; }
        public List<string> LabelList { get; private set; }



        // TODO: sina: make it static and return an instance of EvaluationResult
        public void Evaluate(string actFile, string predFile)
        {
            Evaluate(actFile, predFile, Console.Out);
        }

        // TODO: sina: make it static and return an instance of EvaluationResult
        public void Evaluate(string actFile, string predFile, TextWriter writer)
        {
            bool labeled = false;
            FalseNegativeDic = new Dictionary<string, int>();
            FalsePositiveDic = new Dictionary<string, int>();
            TruePositiveDic = new Dictionary<string, int>();
            TrueNegativeDic = new Dictionary<string, int>();
            LabelList = new List<string>();

            var actIn = new StreamReader(new FileStream(actFile, FileMode.Open));
            actIn.ReadLine();
            actIn.ReadLine();
            actIn.ReadLine();
            string l = actIn.ReadLine();
            if (l.Trim().Length > 0) labeled = true;

            int total = 0;
            int corr = 0;
            int corrL = 0;
            int numsent = 0;
            int corrsent = 0;
            int corrsentL = 0;
            int rootAct = 0;
            int rootGuess = 0;
            int rootCorr = 0;

            actIn.Close();

            actIn = new StreamReader(new FileStream(actFile, FileMode.Open));
            var predIn = new StreamReader(new FileStream(predFile, FileMode.Open));

            actIn.ReadLine();
            string[] pos = actIn.ReadLine().Split("\t".ToCharArray());
            predIn.ReadLine();
            predIn.ReadLine();
            string actLab = labeled ? actIn.ReadLine().Trim() : "";
            string actDep = actIn.ReadLine().Trim();
            string predLab = labeled ? predIn.ReadLine().Trim() : "";
            string predDep = predIn.ReadLine().Trim();
            actIn.ReadLine();
            predIn.ReadLine();

            while (actDep != null)
            {
                string[] actLabs = null;
                string[] predLabs = null;
                if (labeled)
                {

                    actLabs = actLab.Split("\t".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    predLabs = predLab.Split("\t".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                }
                string[] actDeps = actDep.Split("\t".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                string[] predDeps = predDep.Split("\t".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (actDeps.Length != predDeps.Length) Console.WriteLine("Lengths do not match");

                bool whole = true;
                bool wholeL = true;

                for (int i = 0; i < actDeps.Length; i++)
                {

                    if (predDeps[i].Equals(actDeps[i]))
                    {

                        corr++;
                        if (labeled)
                        {
                            if (actLabs[i].Equals(predLabs[i]))
                            {
                                if (!LabelList.Contains(actLabs[i]))
                                    LabelList.Add(actLabs[i]);
                                if (!LabelList.Contains(actLabs[i]))
                                    LabelList.Add(actLabs[i]);
                                corrL++;
                                if (!TruePositiveDic.ContainsKey(actLabs[i]))
                                    TruePositiveDic.Add(actLabs[i], 1);
                                else
                                    TruePositiveDic[actLabs[i]]++;
                            }
                            else wholeL = false;
                        }
                    }
                    else
                    {
                        if (labeled)
                        {
                            if (!FalsePositiveDic.ContainsKey(predLabs[i]))
                                FalsePositiveDic.Add(predLabs[i], 1);
                            else
                                FalsePositiveDic[predLabs[i]]++;
                            if (!FalseNegativeDic.ContainsKey(actLabs[i]))
                                FalseNegativeDic.Add(actLabs[i], 1);
                            else
                                FalseNegativeDic[actLabs[i]]++;
                        }
                        whole = false;
                        wholeL = false;
                    }
                }
                total += actDeps.Length;

                if (whole) corrsent++;
                if (wholeL) corrsentL++;
                numsent++;

                actIn.ReadLine();
                try
                {
                    pos = actIn.ReadLine().Split("\t".ToCharArray());
                }
                catch (Exception e)
                {
                }

                predIn.ReadLine();
                predIn.ReadLine();
                actLab = labeled ? actIn.ReadLine() : "";
                actDep = actIn.ReadLine();
                predLab = labeled ? predIn.ReadLine() : "";
                predDep = predIn.ReadLine();
                actIn.ReadLine();
                predIn.ReadLine();
            }

            writer.WriteLine("Tokens: " + total);
            writer.WriteLine("Correct: " + corr);
           double unlabeledAccuracy = (double) corr/total;
            writer.WriteLine("Unlabeled Accuracy: " + unlabeledAccuracy);
            double unlabeledCompleteAccuracy = (double)corrsent / numsent;
            writer.WriteLine("Unlabeled Complete Correct: " + unlabeledCompleteAccuracy);
            double labeledAccuracy = 0;
            double labeledCompleteAccuracy=0;
            if (labeled)
            {
                labeledAccuracy = (double)corrL / total;
                writer.WriteLine("Labeled Accuracy: " + labeledAccuracy);
                labeledCompleteAccuracy = (double)corrsentL / numsent;
                writer.WriteLine("Labeled Complete Correct: " + labeledCompleteAccuracy);
            }
            foreach (var label in LabelList)
            {
                int truePos = 0;
                if(TruePositiveDic.ContainsKey(label))
                {
                    truePos = TruePositiveDic[label];
                }

                int falsePos = 0;
                if (FalsePositiveDic.ContainsKey(label))
                {
                    falsePos = FalsePositiveDic[label];
                }

                int falseNeg = 0;
                if (FalseNegativeDic.ContainsKey(label))
                {
                    falseNeg = FalseNegativeDic[label];
                }
                int trueNeg = total - (truePos + falseNeg + falsePos);

                double rec = (double) truePos/(falseNeg + truePos);
                double prec = (double) truePos/(truePos + falsePos);
                double f = 2*prec*rec/(prec + rec);
                var outString = label + "\t" + prec + "\t" + rec + "\t" + f;
                writer.WriteLine(outString);
            }
            EvaluationRes=new EvaluationResult(unlabeledAccuracy,unlabeledCompleteAccuracy,labeledAccuracy,labeledCompleteAccuracy);
        }
    }
}