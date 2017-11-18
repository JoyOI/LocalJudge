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
            var meta = JsonConvert.DeserializeObject<Meta>(File.ReadAllText("limit.json"));
            var p = Process.Start(new ProcessStartInfo("runner") { RedirectStandardInput = true });
            if (File.Exists("Main.class"))
            {
                p.StandardInput.WriteLine($"{ meta.UserTime } { meta.PhysicalTime } 0");
                p.StandardInput.WriteLine("java Main -Xms128m -Xmx256m");
            }
            else
            {
                p.StandardInput.WriteLine($"{ meta.UserTime } { meta.PhysicalTime }");
                p.StandardInput.WriteLine("./Main.out");
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