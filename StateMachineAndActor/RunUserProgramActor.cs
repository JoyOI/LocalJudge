using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;

namespace StateMachineAndActor.JoyOI
{
    class Meta
    {
        public int PhysicalTime { get; set; }

        public int UserTime { get; set; }
    }

    class RunUserProgramActor
    {
        static void Main(string[] args)
        {
            Prepare();
            Run();
        }

        static void Prepare()
        {
            if (File.Exists("Main.dll"))
            {
                const string runtimeConfig = @"{ ""runtimeOptions"": { ""tfm"": ""netcoreapp2.0"", ""framework"": { ""name"": ""Microsoft.NETCore.App"", ""version"": ""2.0.0"" } } }";
                File.WriteAllText("Main.runtimeconfig.json", runtimeConfig);
            }
        }

        static void Run()
        {
            var meta = JsonConvert.DeserializeObject<Meta>(File.ReadAllText("limit.json"));
            var p = Process.Start(new ProcessStartInfo("runner") { RedirectStandardInput = true });
            if (File.Exists("Main.jar"))
            {
                p.StandardInput.WriteLine($"{ meta.UserTime } { meta.PhysicalTime } 0");
                p.StandardInput.WriteLine("java -jar Main.jar -Xms128m -Xmx256m");
            }
            else if (File.Exists("Main.dll"))
            {
                p.StandardInput.WriteLine($"{ meta.UserTime } { meta.PhysicalTime } 0");
                p.StandardInput.WriteLine("dotnet Main.dll");
            }
            else if (File.Exists("Main.out"))
            {
                p.StandardInput.WriteLine($"{ meta.UserTime } { meta.PhysicalTime }");
                p.StandardInput.WriteLine("./Main.out");
            }
            else if (File.Exists("Main.py"))
            {
                p.StandardInput.WriteLine($"{ meta.UserTime } { meta.PhysicalTime } 0");
                p.StandardInput.WriteLine("python3 Main.py");
            }
            else
            {
                throw new FileNotFoundException("The executable file not found.");
            }
            p.StandardInput.Close();
            p.WaitForExit();
            var json = JsonConvert.SerializeObject(new
            {
                Outputs = new string[] { "runner.json", "stdout.txt" }
            });
            File.WriteAllText("return.json", json);
        }
    }
}