-------------------------
MSTParser C# conversion of JAVA version 0.2
-------------------------

The following package contains a C# conversion of the original java implementation of the dependency parsers
described in:

Non-Projective Dependency Parsing using Spanning Tree Algorithms
R. McDonald, F. Pereira, K. Ribarov and J. Hajic
HLT-EMNLP, 2005

Online Large-Margin Training of Dependency Parsers
R. McDonald, K. Crammer and F. Pereira
ACL, 2005

Online Learning of Approximate Dependency Parsing Algorithms
R. McDonald and F. Pereira
EACL, 2006

In addition, the parsers in this package can also learn and produce typed
dependency trees (i.e. trees with edge labels).

The parser should work with C# 3.5 or upper versions.

If there are any problems running the parser then email: rasooli.ms{AT}gmail.com
Since the conversion is done manually maybe there are some errors that I did not notice them, so do not hesitate to contact with me.


----------------
Contents
----------------

1. Example of usage

2. Running the parser
   a. Input data format
   b. Training a parser
   c. Running a trained model on new data
   d. Evaluating output


---------------------
1. Example Usage
---------------------
In the MSTParserCSharp package in Program.cs a complete crossvalidation is shown by a sample Persian treebank (the full treebank can be obtained freely in http://dadegan.ir/en).

-------------------------
2. Running the Parser
-------------------------

-------------------------
2a. Input data format
-------------------------

Example data sets are given in the data/ directory.

Each sentence in the data is represented by 3 or 4 lines and sentences are
space separated. The general format is:

w1    w2    ...    wn
p1    p2    ...    pn
l1    l2    ...    ln
d1    d2    ...    d2

....


Where,
- w1 ... wn are the n words of the sentence (tab deliminated)
- p1 ... pn are the POS tags for each word
- l1 ... ln are the labels of the incoming edge to each word
- d1 ... dn are integers representing the postition of each words parent

For example, the sentence "John hit the ball" would be:

John	hit	the	ball
N	V	D	N
SBJ	ROOT	MOD	OBJ
2	0	4	2

Note that hit's parent is indexed by 0 since it is the root.

If you wish to only train or test an unlabeled parser, then simply leave out
the third line for each sentence, e.g.,

John	hit	the	ball
N	V	D	N
2	0	4	2

The parser will automatically detect that it should produce unlabeled trees.

Note that this format is the same for training AND for running the parser on
new data. Of course, you may not always know the gold standard. In this case,
just substitute lines 3 (the edge labels) and lines 4 (the parent indexes) with
dummy values. The parser just ignores these values and produces its own.


----------------------------
2b. Training the parser
----------------------------

If you have a set of labeled data, first place it in the format described
above.

If your training data is in a file "trainFile", you can then run the command:

public static void Train(string trainFile, string modelName, int numOfTrainingIterations, bool isProjective, int trainingK, bool createForest, int order)

This will train a parser with all the default properties. Additonal
properties can be described with the following flags:

train
- if present then parser will train a new model


modelName
- store trained model in file called model.name

numOfTrainingIterations
- Run training algorithm for numIters epochs, default is 10

isProjective
- type is either "proj"=true or "non-proj"=false, e.g. decode-type:proj
- Default is "proj"
- "proj" use the projective parsing algorithm during training
  - i.e. The Eisner algorithm
- "non-proj" use the non-projective parsing algorithm during training
  - i.e. The Chu-Liu-Edmonds algorithm

trainingK
- Specifies the k-best parse set size to create constraints during training
- Default is 1
- For non-projective parsing algorithm, k-best decoding is approximate

createForest
- cf is either "true" or "false"
- Default is "true"
- If create-forest is false, it will not create the training parse forest (see
  section 4). It assumes it has been created.
- This flag is useful if you are training many models on the same data and
  features but using different parameters (e.g. training iters, decoding type).

order
- ord is either 1 or 2
- Default is 1
- Specifies the order/scope of features. 1 only has features over single edges
  and 2 has features over pairs of adjacent edges in the tree.


------------------------------------------------
2c. Running a trained model on new data
------------------------------------------------

This section assumes you have trained a model and it is stored in dep.model.

First, format your data properly (section 2a).

It should be noted that the parser assumes both words and POS tags. To
generate POS tags for your data I suggest using the Ratniparkhi POS tagger
or another tagger of your choice.

The parser also assumes that the edge label and parent index lines are
in the input. However, these can just be artificially inserted (e.g. with lines
of "LAB ... LAB" and "0 ... 0") since the parser will produce these lines
as output.

If the data is in a file called test.txt, run the command:

public static void Test(string testFile, string modelName, string outFile, int order)

This will create an output file "outFile" with the predictions of the parser.
Other properties can be defined with the following flags:

testFile
- The file containing the data to run the parser on

modelName
- The name of the stored model to be used

outFile
- The result of running the parser on the new data


order
- See section 2b. THIS NEEDS TO HAVE THE SAME VALUE OF THE TRAINED MODEL!!

Note that if you train a labeled model, you should only run it expecting
labeled output (e.g. the test data should have 4 lines per sentence).
And if you train an unlabeled model, you should only run it expecting
unlabeled output (e.g. the test data should have 3 lines per sentence).


------------------------
2d. Evaluating Output
------------------------

This section describes a simple class for evaluating the output of
the parser against a gold standard.

Assume you have a gold standard, say test.txt and the output of the parser
say out.txt, then run the following command:

public static EvaluationResult Evaluate(string goldFile, string outFile)

This will return both labeled and unlabeled accuracy (if the data sets contain
labeled trees) as well as complete sentence accuracy, again labeled and
unlabeled.

We should note that currently this evaluation script includes all punctuation.
In future releases we will modify this class to allow for the evaluation to
ingnore punctuation, which is standard for English (Yamada and Matsumoto 03).

