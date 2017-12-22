using System;
using System.IO;
using Newtonsoft.Json;

namespace StateMachineAndActor.Test
{
    public static class EmptyActor
    {
        public static void Main()
        {
            Console.WriteLine("Empty");
            var json = JsonConvert.SerializeObject(new
            {
                Outputs = new string[] {  }
            });
            File.WriteAllText("return.json", json);
        }
    }
}
