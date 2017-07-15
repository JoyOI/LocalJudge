using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;

namespace JoyOI.ManagementService.Playground
{
    class TyvjCompareActor
    {
        static void Main(string[] args)
        {
            var p = Process.Start(new ProcessStartInfo("runner") { RedirectStandardInput = true });
            p.StandardInput.WriteLine("5000");
            p.StandardInput.WriteLine("./Validator.out");
            p.WaitForExit();
            var json = JsonConvert.SerializeObject(new
            {
                Outputs = new string[] { "runner.json", "stdout.txt" }
            });
            File.WriteAllText("return.json", json);
        }
    }
}