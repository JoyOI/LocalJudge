using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;

namespace JoyOI.ManagementService.Playground
{
    class TyvjCompileActor
    {
        static void Main(string[] args)
        {
            var p = Process.Start(new ProcessStartInfo("runner") { RedirectStandardInput = true });
            p.StandardInput.WriteLine("5000 5000");
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Main.c")))
                p.StandardInput.WriteLine("gcc -O2 -o Main.out -DONLINE_JUDGE -lm --static --std=c99 Main.c");
            else if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Main.cpp")))
                p.StandardInput.WriteLine("g++ -O2 -o Main.out -DONLINE_JUDGE -lm --static --std=c++98 Main.cpp");
            else if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Main.pas")))
                p.StandardInput.WriteLine("fpc -O2 -o Main.out -dONLINE_JUDGE Main.pas");
            else
                throw new NotSupportedException("Your source code does not support to compile.");
            p.WaitForExit();
            if (File.Exists("Main.out"))
            {
                var json = JsonConvert.SerializeObject(new
                {
                    Outputs = new string[] { "runner.json", "Main.out", "stdout.txt", "stderr.txt" }
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