using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;

namespace JoyOI.ManagementService.Playground
{
    class TyvjRunUserProgramActor
    {
        static void Main(string[] args)
        {
            var p = Process.Start(new ProcessStartInfo("runner") { RedirectStandardInput = true });
            p.StandardInput.WriteLine("1000 2000");
            if (File.Exists("Main.class"))
                p.StandardInput.WriteLine("java Main -Xms128m -Xmx256m");
            else
                p.StandardInput.WriteLine("./Main.out");
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