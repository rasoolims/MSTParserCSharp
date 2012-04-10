using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MSTParser;

namespace MSTParserCSharp
{
    class Program
    {
        static void Main(string[] args)
        {
            string dataFolderPath = "../../../Data";
            CrossValidate(Path.Combine(dataFolderPath, "zakhireh-mst-train"), 10);
            Console.ReadLine();
        }

        public static void CrossValidate(string path, int num)
        {
            var reader = new StreamReader(path);
            string sentence = "";
            var senList = new List<string>();
            var senBuilder = new StringBuilder();

            while ((sentence = reader.ReadLine()) != null)
            {
                if (sentence.Trim() != "")
                {
                    senBuilder.AppendLine(sentence.Trim());
                }
                else
                {
                    senList.Add(senBuilder.ToString().Trim());
                    senBuilder = new StringBuilder();
                }
            }

            var prop = senList.Count / num;

            for (int i = 0; i < num; i++)
            {
                var trainPath = "train." + i + 1 + ".txt";
                var testPath = "test." + i + 1 + ".txt";

                var trainWriter = new StreamWriter(trainPath);
                var testWriter = new StreamWriter(testPath);


                for (int j = 0; j < senList.Count; j++)
                {
                    if ((j % num) == i)
                    {
                        testWriter.WriteLine(senList[j] + "\r\n");
                    }
                    else
                    {
                        trainWriter.WriteLine(senList[j] + "\r\n");
                    }
                }
                trainWriter.Flush();
                trainWriter.Close();
                testWriter.Flush();
                testWriter.Close();

                MSTParser.MSTParser.Train(
                Path.Combine("", trainPath),
                Path.Combine("", "model.dep"),
                5, false, 1, true, 1);

                MSTParser.MSTParser.Test(
                    Path.Combine("", testPath),
                    Path.Combine("", "model.dep"),
                    Path.Combine("", "out." + i + 1 + ".txt"), 1);

                EvaluationResult evaluationResult = MSTParser.MSTParser.Evaluate(
                    Path.Combine("", testPath),
                    Path.Combine("", "out." + i + 1 + ".txt"));
            }

        }

    }

}
