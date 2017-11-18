using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;

namespace JoyOI.ManagementService.Actors
{
    class CompileActor
    {
        static void Main(string[] args)
        {
            var compileOutputFilename = "Main.out";
            var p = Process.Start(new ProcessStartInfo("runner") { RedirectStandardInput = true });
            var limitationInput = "5000 5000";
            var commandInput = "";

            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Main.c")))
                commandInput = "gcc -O2 -o Main.out -DONLINE_JUDGE -lm --static --std=c99 Main.c";
            else if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Main.cpp")))
                commandInput = "g++ -O2 -o Main.out -DONLINE_JUDGE -lm --static --std=c++98 Main.cpp";
            else if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Main11.cpp")))
                commandInput = "g++ -O2 -o Main.out -DONLINE_JUDGE -lm --static --std=c++11 Main11.cpp";
            else if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Main14.cpp")))
                commandInput = "g++ -O2 -o Main.out -DONLINE_JUDGE -lm --static --std=c++14 Main14.cpp";
            else if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Main.pas")))
                commandInput = "fpc -O2 -oMain.out -dONLINE_JUDGE Main.pas";
            else if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Main.java")))
            {
                commandInput = "javac Main.java -J-Xms128m -J-Xmx256m";
                compileOutputFilename = "Main.class";
                limitationInput = "5000 10000 0";
            }
            else
            {
                throw new NotSupportedException("Your source code does not support to compile.");
            }

            p.StandardInput.WriteLine(limitationInput);
            p.StandardInput.WriteLine(commandInput);
            p.WaitForExit();

            if (File.Exists(compileOutputFilename))
            {
                var json = JsonConvert.SerializeObject(new
                {
                    Outputs = new string[] { "runner.json", compileOutputFilename, "stdout.txt", "stderr.txt" }
                });
                File.WriteAllText("return.json", json);
            }
            else
            {
                var json = JsonConvert.SerializeObject(new
                {
                    Outputs = new string[] { "runner.json", "stdout.txt", "stderr.txt" }
                });
                File.WriteAllText("return.json", json);
            }
        }
    }
}