using JoyOI.ManagementService.Core;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Migrations;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace StateMachineAndActor.JoyOI
{
    public class RunnerReturn
    {
        public int UserTime { get; set; }
        public int TotalTime { get; set; }
        public int ExitCode { get; set; }
        public int PeakMemory { get; set; }
        public bool IsTimeout { get; set; }
        public string Error { get; set; }
    }

    public class ManagementServiceCallBack
    {
        public Guid StateMachineId { get; set; }
        public Guid? InputBlobId { get; set; }
        public string Status { get; set; }
        public int Memory { get; set; }
        public int Time { get; set; }
        public string Hint { get; set; }
    }

    public class CompileOnlyStateMachine : StateMachineBase
    {
        public override async Task RunAsync()
        {
            switch (Stage)
            {
                case "Compile":
                    await SetStageAsync("Compile");
                    // 开始部署编译Actor
                    await DeployAndRunActorAsync(new RunActorParam("CompileActor", InitialBlobs.Where(x => x.Name.StartsWith("Main"))));
                    break;
            }
        }
    }
}
