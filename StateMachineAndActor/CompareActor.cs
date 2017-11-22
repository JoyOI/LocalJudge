using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;

namespace StateMachineAndActor.JoyOI
{
    class CompareActor
    {
        static void Main(string[] args)
        {
            Prepare();
            Compare();
        }

        static void Prepare()
        {
            if (File.Exists("Validator.dll"))
            {
                const string runtimeConfig = @"{ ""runtimeOptions"": { ""tfm"": ""netcoreapp2.0"", ""framework"": { ""name"": ""Microsoft.NETCore.App"", ""version"": ""2.0.0"" } } }";
                File.WriteAllText("Validator.runtimeconfig.json", runtimeConfig);
            }
        }

        static void Compare()
        {
            var p = Process.Start(new ProcessStartInfo("runner") { RedirectStandardInput = true });
            if (File.Exists("Validator.out"))
            {
                p.StandardInput.WriteLine("5000 5000");
                p.StandardInput.WriteLine("./Validator.out");
            }
            else if (File.Exists("Validator.dll"))
            {
                p.StandardInput.WriteLine("5000 10000 0");
                p.StandardInput.WriteLine("dotnet Validator.dll");
            }
            else if (File.Exists("Validator.class"))
            {
                p.StandardInput.WriteLine("5000 10000 0");
                p.StandardInput.WriteLine("java Validator -Xms128m -Xmx256m");
            }
            else if (File.Exists("Validator.py"))
            {
                p.StandardInput.WriteLine("5000 10000 0");
                p.StandardInput.WriteLine("python3 Validator.py");
            }
            else
            {
                throw new FileNotFoundException("Validator not found.");
            }
            p.WaitForExit();
            var json = JsonConvert.SerializeObject(new
            {
                Outputs = new string[] { "runner.json", "stdout.txt" }
            });
            File.WriteAllText("return.json", json);
        }
    }
}