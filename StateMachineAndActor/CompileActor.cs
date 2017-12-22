using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace JoyOI.ManagementService.Actors
{
    class CompileActor
    {
        static void Main(string[] args)
        {
            Prepare();
            Compile();
        }

        static void Prepare()
        {
            // Prepare for c#
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Main.cs")))
            {
                const string csproj = @"<Project Sdk=""Microsoft.NET.Sdk""><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>netcoreapp2.0</TargetFramework></PropertyGroup></Project>";
                File.WriteAllText("Main.csproj", csproj);
            }
            else if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Main.vb")))
            {
                const string vbproj = @"<Project Sdk=""Microsoft.NET.Sdk""><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>netcoreapp2.0</TargetFramework></PropertyGroup></Project>";
                File.WriteAllText("Main.vbproj", vbproj);
            }
            else if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Main.fs")))
            {
                const string fsproj = @"<Project Sdk=""Microsoft.NET.Sdk""><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>netcoreapp2.0</TargetFramework></PropertyGroup></Project>";
                File.WriteAllText("Main.fsproj", fsproj);
            }
            // Prepare for python
            else if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Main.py")))
            {
                File.WriteAllText("runner.json", "{ \"ExitCode\": 0 }");
                var json = JsonConvert.SerializeObject(new
                {
                    Outputs = new string[] { "Main.py", "runner.json" }
                });
                File.WriteAllText("return.json", json);
                Environment.Exit(0);
            }
        }

        static void Compile()
        {
            var compileOutputFilename = "Main.out";
            var p = Process.Start(new ProcessStartInfo("runner") { RedirectStandardInput = true });
            var limitationInput = "5000 5000";
            var commandInput = "";

            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Main.c")))
                commandInput = "gcc -Os -o Main.out -DONLINE_JUDGE -lm --std=c99 Main.c";
            else if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Main.cpp")))
                commandInput = "g++ -Os -o Main.out -DONLINE_JUDGE -lm --std=c++98 Main.cpp";
            else if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Main11.cpp")))
                commandInput = "g++ -Os -o Main.out -DONLINE_JUDGE -lm --std=c++11 Main11.cpp";
            else if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Main14.cpp")))
                commandInput = "g++ -Os -o Main.out -DONLINE_JUDGE -lm --std=c++14 Main14.cpp";
            else if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Main.pas")))
                commandInput = "fpc -Og -oMain.out -dONLINE_JUDGE Main.pas";
            else if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Main.java")))
            {
                commandInput = "javac-jar Main.java Main Main.jar";
                compileOutputFilename = "Main.jar";
                limitationInput = "5000 15000 0";
            }
            else if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Main.cs")))
            {
                commandInput = "dotnet build -o ./ -c Release";
                compileOutputFilename = "Main.dll";
                limitationInput = "5000 15000 0";
            }
            else if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Main.vb")))
            {
                commandInput = "dotnet build -o ./ -c Release";
                compileOutputFilename = "Main.dll";
                limitationInput = "5000 15000 0";
            }
            else if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Main.fs")))
            {
                commandInput = "dotnet build -o ./ -c Release";
                compileOutputFilename = "Main.dll";
                limitationInput = "5000 15000 0";
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