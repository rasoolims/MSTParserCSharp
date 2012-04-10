using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MSTParser
{
    public class MSTParser
    {
        private const int DefaultNumOfIterations = 10;

        #region Train

        /// <summary>
        /// Train the paser with train file.
        /// </summary>
        /// <param name="trainFile">Training file path</param>
        /// <param name="modelName">Model Path</param>
        /// <param name="numOfTrainingIterations">Number of iteration for training feature weights; Default=10</param>
        /// <param name="isProjective">Type of Parsing; Default is True; Free order languages need to be nonprojective
        /// True: use the projective parsing algorithm during training i.e. The Eisner algorithm
        /// False: use the non-projective parsing algorithm during training i.e. The Chu-Liu-Edmonds algorithm</param>
        /// <param name="trainingK"> Specifies the k-best parse set size to create constraints during training
        /// Default is 1
        /// For non-projective parsing algorithm, k-best decoding is approximate</param>
        /// <param name="createForest">Default is "true"
        /// If create-forest is false, it will not create the training parse forest . It assumes it has been created.
        /// This flag is useful if you are training many models on the same data and
        /// features but using different Parameters (e.g. training iters, decoding type).</param>
        /// <param name="order">is either 1 or 2
        /// Default is 1
        /// Specifies the order/scope of features. 1 only has features over single edges
        /// and 2 has features over pairs of adjacent edges in the tree</param>
        public static void Train(string trainFile, string modelName, int numOfTrainingIterations, bool isProjective, int trainingK, bool createForest, int order)
        {
            DependencyParser.SecondOrder = (order == 2 ? true : false);
            DependencyParser.DecodeType = isProjective ? ProjectiveTypes.Projective : ProjectiveTypes.NonProjective;

            DependencyPipe pipe =
                order == 2 ? new DependencyPipe2O(createForest) : new DependencyPipe(createForest);
            DependencyParser.SecondOrder = order == 2 ? true : false;
            pipe.SetLabeled(trainFile);

            string trainForest = trainFile + ".forest";
            DependencyInstance[] trainingData = pipe.CreateInstances(trainFile, trainForest);
            pipe.CloseAlphabets();
            var dp = new DependencyParser(pipe);

            dp.Train(trainingData, trainFile, trainForest);
            dp.SaveModel(modelName);
        }

        /// <summary>
        /// Train the paser with train file, order is by default=1
        /// </summary>
        /// <param name="trainFile">Training file path</param>
        /// <param name="modelName">Model Path</param>
        /// <param name="numOfTrainingIterations">Number of iteration for training feature weights; Default=10</param>
        /// <param name="isProjective">Type of Parsing; Default is True; Free order languages need to be nonprojective
        /// True: use the projective parsing algorithm during training i.e. The Eisner algorithm
        /// False: use the non-projective parsing algorithm during training i.e. The Chu-Liu-Edmonds algorithm</param>
        /// <param name="trainingK"> Specifies the k-best parse set size to create constraints during training
        /// Default is 1
        /// For non-projective parsing algorithm, k-best decoding is approximate</param>
        /// <param name="createForest">Default is "true"
        /// If create-forest is false, it will not create the training parse forest . It assumes it has been created.
        /// This flag is useful if you are training many models on the same data and
        /// features but using different Parameters (e.g. training iters, decoding type).</param>
        public static void Train(string trainFile, string modelName, int numOfTrainingIterations, bool isProjective, int trainingK, bool createForest)
        {
            Train(trainFile, modelName, numOfTrainingIterations, isProjective, trainingK, createForest, 1);
        }

        /// <summary>
        /// Train the paser with train file, order is by default=1, createforest=true
        /// </summary>
        /// <param name="trainFile">Training file path</param>
        /// <param name="modelName">Model Path</param>
        /// <param name="numOfTrainingIterations">Number of iteration for training feature weights; Default=10</param>
        /// <param name="isProjective">Type of Parsing; Default is True; Free order languages need to be nonprojective
        /// True: use the projective parsing algorithm during training i.e. The Eisner algorithm
        /// False: use the non-projective parsing algorithm during training i.e. The Chu-Liu-Edmonds algorithm</param>
        /// <param name="trainingK"> Specifies the k-best parse set size to create constraints during training
        /// Default is 1
        /// For non-projective parsing algorithm, k-best decoding is approximate</param>
        public static void Train(string trainFile, string modelName, int numOfTrainingIterations, bool isProjective, int trainingK)
        {
            Train(trainFile, modelName, numOfTrainingIterations, isProjective, trainingK, true, 1);
        }

        /// <summary>
        /// Train the paser with train file, order is by default=1, createforest=true,trainingK=1
        /// </summary>
        /// <param name="trainFile">Training file path</param>
        /// <param name="modelName">Model Path</param>
        /// <param name="numOfTrainingIterations">Number of iteration for training feature weights; Default=10</param>
        /// <param name="isProjective">Type of Parsing; Default is True; Free order languages need to be nonprojective
        /// True: use the projective parsing algorithm during training i.e. The Eisner algorithm
        /// False: use the non-projective parsing algorithm during training i.e. The Chu-Liu-Edmonds algorithm</param>
        public static void Train(string trainFile, string modelName, int numOfTrainingIterations, bool isProjective)
        {
            Train(trainFile, modelName, numOfTrainingIterations, isProjective, 1, true, 1);
        }

        /// <summary>
        /// Train the paser with train file, order is by default=1, createforest=true,trainingK=1,isProjective=true
        /// </summary>
        /// <param name="trainFile">Training file path</param>
        /// <param name="modelName">Model Path</param>
        /// <param name="numOfTrainingIterations">Number of iteration for training feature weights; Default=10</param>
        public static void Train(string trainFile, string modelName, int numOfTrainingIterations)
        {
            DependencyParser.NumIters = numOfTrainingIterations;
            Train(trainFile, modelName, numOfTrainingIterations, false, 1, true, 2);
        }

        /// <summary>
        /// Train the paser with train file, order is by default=1, createforest=true,trainingK=1,isProjective=true,numOfTrainingIterations=10
        /// </summary>
        /// <param name="trainFile">Training file path</param>
        /// <param name="modelName">Model Path</param>
        public static void Train(string trainFile, string modelName)
        {
            Train(trainFile, modelName, DefaultNumOfIterations, true, 1, true,1);
        }

        #endregion

        #region Test

        /// <summary>
        /// Tests the specified test file.
        /// </summary>
        /// <param name="testFile">The test file.</param>
        /// <param name="modelName">Model Path</param>
        /// <param name="outFile">The parser output path</param>
        /// <param name="order">is either 1 or 2
        /// Default is 1
        /// Specifies the order/scope of features. 1 only has features over single edges
        /// and 2 has features over pairs of adjacent edges in the tree</param>
        public static void Test(string testFile, string modelName, string outFile, int order)
        {
            DependencyParser.SecondOrder = (order == 2 ? true : false);
            DependencyPipe pipe =
                    order == 2 ? new DependencyPipe2O(true) : new DependencyPipe(true);
            pipe.SetLabeled(testFile);
            var dp = new DependencyParser(pipe);

            dp.LoadModel(modelName);

            pipe.CloseAlphabets();

            dp.OutputParses(testFile, outFile);
        }
        /// <summary>
        /// Tests the specified test file, order is by default= 1
        /// </summary>
        /// <param name="testFile">The test file.</param>
        /// <param name="modelName">Model Path</param>
        /// <param name="outFile">The parser output path</param>
        public static void Test(string testFile, string modelName, string outFile)
        {
            Test(testFile, modelName, outFile, 2);
        }
        #endregion

        #region Evaluation
        /// <summary>
        /// Evaluates the specified gold file (The file with Correct Dependecy Labels) with out file (the output file of the parser)
        /// </summary>
        /// <param name="goldFile">The gold file path</param>
        /// <param name="outFile">The out file path</param>
        public static EvaluationResult Evaluate(string goldFile, string outFile)
        {
            var evaluator = new DependencyEvaluator();
            evaluator.Evaluate(goldFile, outFile);
            var res = evaluator.EvaluationRes;
            return res;
        }
        #endregion

        #region Parse
        /// <summary>
        /// Parses the specified words with the specified trained parser.
        /// </summary>
        /// <param name="words">The words.</param>
        /// <param name="posTags">The pos tags.</param>
        /// <param name="parser">The trained parser.</param>
        /// <param name="labels">The labels.</param>
        /// <param name="deps">The deps.</param>
        public static void Parse(string[] words, string[] posTags, DependencyParser parser, out string[] labels, out int[] deps)
        {
            parser.OutputParses(words, posTags, out labels, out deps);
        }
        /// <summary>
        /// Parses the specified sentence composed of words.
        /// </summary>
        /// <param name="words">The words.</param>
        /// <param name="posTags">The pos tags.</param>
        /// <param name="modelName">Name of the model.</param>
        /// <param name="labeled">if set to <c>true</c> [labeled].</param>
        /// <param name="order">The order.</param>
        /// <param name="labels">The labels as an output.</param>
        /// <param name="deps">The deps as an output.</param>
        public static void Parse(string[] words, string[] posTags, string modelName, bool labeled, int order, out string[] labels, out int[] deps)
        {
            DependencyPipe pipe =
                order == 2 ? new DependencyPipe2O(true) : new DependencyPipe(true);
            pipe.setLabel(labeled);
            var dp = new DependencyParser(pipe);

            dp.LoadModel(modelName);

            pipe.CloseAlphabets();

            dp.OutputParses(words, posTags, out labels, out deps);
        }
        /// <summary>
        /// Parses the specified words, order is by default 2
        /// </summary>
        /// <param name="words">The words.</param>
        /// <param name="posTags">The pos tags.</param>
        /// <param name="modelName">Name of the model.</param>
        /// <param name="labeled">if set to <c>true</c> [labeled].</param>
        /// <param name="labels">The labels.</param>
        /// <param name="deps">The deps.</param>
        public static void Parse(string[] words, string[] posTags, string modelName, bool labeled, out string[] labels, out int[] deps)
        {
            DependencyPipe pipe = new DependencyPipe2O(true);
            pipe.setLabel(labeled);
            var dp = new DependencyParser(pipe);

            dp.LoadModel(modelName);

            pipe.CloseAlphabets();

            dp.OutputParses(words, posTags, out labels, out deps);
        }
        /// <summary>
        /// Parses the specified words. Create forest is by default true
        /// </summary>
        /// <param name="words">The words.</param>
        /// <param name="posTags">The pos tags.</param>
        /// <param name="modelName">Name of the model.</param>
        /// <param name="order">The order.</param>
        /// <param name="labels">The labels.</param>
        /// <param name="deps">The deps.</param>
        public static void Parse(string[] words, string[] posTags, string modelName, int order, out string[] labels, out int[] deps)
        {
            DependencyPipe pipe =
                order == 2 ? new DependencyPipe2O(true) : new DependencyPipe(true);
            pipe.setLabel(true);
            var dp = new DependencyParser(pipe);
            dp.LoadModel(modelName);
            pipe.CloseAlphabets();
            dp.OutputParses(words, posTags, out labels, out deps);
        }
        /// <summary>
        /// Parses the specified words, order is by default 2 and labeled is true
        /// </summary>
        /// <param name="words">The words.</param>
        /// <param name="posTags">The pos tags.</param>
        /// <param name="modelName">Name of the model.</param>
        /// <param name="labels">The labels.</param>
        /// <param name="deps">The deps.</param>
        public static void Parse(string[] words, string[] posTags, string modelName, out string[] labels, out int[] deps)
        {
            DependencyPipe pipe = new DependencyPipe2O(true);
            pipe.setLabel(true);
            var dp = new DependencyParser(pipe);
            dp.LoadModel(modelName);
            pipe.CloseAlphabets();
            dp.OutputParses(words, posTags, out labels, out deps);
        }
        #endregion
    }
}
