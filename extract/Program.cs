using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace extract
{
    class Program
    {
        static void Main(string[] args)
        {
            var parsedArgs = Parser.Default.ParseArguments<FileRegexProcessingOptions, CompareFileRegexProcessingOptions>(args)
                                   .MapResult(
                                        (FileRegexProcessingOptions opts) => FileRegexProcessingOptions.Process(opts),
                                        (CompareFileRegexProcessingOptions opts) => CompareFileRegexProcessingOptions.Process(opts),
                                        errs => 1
                                    );
        }
    }



    [Verb("file", HelpText = "Processes a file or files using regex. Expose the regex group names as replacement tokens")]
    public class FileRegexProcessingOptions
    {
        [Option('d', "directory", Required = false,
            HelpText = "The directory in which to search for files")]
        public string Dir { get; set; }

        [Option('f', "files", Required = true, 
            HelpText="Wildcard enabled file name identifying the file or files to be processed")]
        public string FileName { get; set; }

        [Option('r', "regex", Required = true,
            HelpText = "The regular expression pattern used to extract values from the file")]
        public string Pattern { get; set; }

        [Option('t', "template", Required = true,
            HelpText = "The template text using { } braces to identify replacement sections. These sections should match regex named groups")]
        public string Template { get; set; }

        [Option('u', "unique", Required = false,
            HelpText = "Only return unique results")]
        public bool Unique { get; set; }

        public static int Process(FileRegexProcessingOptions opts)
        {
            HashSet<string> hash = new HashSet<string>();
            var directory = opts.Dir;
            if (String.IsNullOrEmpty(directory))
            {
                directory = ".";
            }
            var files = Directory.GetFiles(directory, opts.FileName);
            var pattern = new Regex(opts.Pattern);
            var groupNames = pattern.GetGroupNames();
            
            foreach(var f in files)
            {
                string content = File.ReadAllText(f);
                var matches = pattern.Matches(content);

                foreach(Match match in matches)
                {
                    var output = opts.Template;
                    foreach(var g in groupNames)
                    {
                        output = output.Replace($"{{{g}}}", match.Groups[g].Value);
                    }

                    if (opts.Unique && !hash.Contains(output))
                    {
                        hash.Add(output);
                        Console.WriteLine(output);
                    }
                    else if (!opts.Unique)
                    {
                        Console.WriteLine(output);
                    }

                }
            }

            Console.ReadKey();

            return 0;
        }
    }

    [Verb("compare", HelpText = "Finds the differences in matches between the source and the destination")]
    public class CompareFileRegexProcessingOptions
    {
        [Option('d', "directory", Required = false,
            HelpText = "The directory in which to search for files")]
        public string Dir { get; set; }

        [Option('f', "files", Required = true,
            HelpText = "Wildcard enabled file name identifying the file or files to be checked")]
        public string FileName { get; set; }

        [Option('c', "compareto", Required = true,
            HelpText = "Wildcard enabled file name identifying the file or files to be compared to")]
        public string CompareFileName { get; set; }

        [Option('r', "regex", Required = true,
            HelpText = "The regular expression pattern used to extract values from the file")]
        public string Pattern { get; set; }

        [Option('t', "template", Required = true,
            HelpText = "The template text using { } braces to identify comparisons. These sections should match regex named groups")]
        public string Template { get; set; }

        public static int Process(CompareFileRegexProcessingOptions opts)
        {
            var directory = opts.Dir;
            if (String.IsNullOrEmpty(directory))
            {
                directory = ".";
            }
            var files = Directory.GetFiles(directory, opts.FileName);
            var pattern = new Regex(opts.Pattern);
            var groupNames = pattern.GetGroupNames();

            Dictionary<string, int> sourceResults = new Dictionary<string, int>();
            Dictionary<string, int> compareResults = new Dictionary<string, int>();

            //Build sourceResults
            foreach (var f in files)
            {
                string content = File.ReadAllText(f);
                var matches = pattern.Matches(content);

                foreach (Match match in matches)
                {
                    var output = opts.Template;
                    foreach (var g in groupNames)
                    {
                        output = output.Replace($"{{{g}}}", match.Groups[g].Value);
                    }

                    if (sourceResults.ContainsKey(output))
                        sourceResults[output] = sourceResults[output]++;
                    else
                        sourceResults.Add(output, 1);
                }
            }

            //Build compareResults
            files = Directory.GetFiles(directory, opts.CompareFileName);
            foreach (var f in files)
            {
                string content = File.ReadAllText(f);
                var matches = pattern.Matches(content);

                foreach (Match match in matches)
                {
                    var output = opts.Template;
                    foreach (var g in groupNames)
                    {
                        output = output.Replace($"{{{g}}}", match.Groups[g].Value);
                    }

                    if (compareResults.ContainsKey(output))
                        compareResults[output] = compareResults[output]++;
                    else
                        compareResults.Add(output, 1);
                }
            }

            int countDiff = 0;
            int countMissing = 0;
            Console.WriteLine($"Differences {opts.FileName} in {opts.CompareFileName}?");
            foreach(var k in sourceResults.Keys)
            {
                if (compareResults.ContainsKey(k) && compareResults[k] != sourceResults[k])
                {
                    Console.WriteLine($"Count  - {compareResults[k]} - {sourceResults[k]} - {k}");
                    countDiff++;
                }
                else if (!compareResults.ContainsKey(k))
                {
                    Console.WriteLine($"Missing  - {k}");
                    countMissing++;
                }
            }
            Console.WriteLine("----------------------------------");
            Console.WriteLine($"Differences {opts.CompareFileName} in {opts.FileName}?");
            foreach (var k in compareResults.Keys)
            {
                if (!sourceResults.ContainsKey(k))
                {
                    Console.WriteLine($"Missing - {k}");
                    countMissing++;
                }
            }

            Console.WriteLine($"{opts.FileName} Count: {sourceResults.Count}");
            Console.WriteLine($"{opts.CompareFileName} Count: {compareResults.Count}");
            Console.WriteLine($"Missing: {countMissing}");
            Console.WriteLine($"Diff   : {countDiff}");

            Console.ReadKey();

            return 0;
        }
    }
}
