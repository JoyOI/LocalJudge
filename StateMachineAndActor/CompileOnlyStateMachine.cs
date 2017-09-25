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
    public class CompileOnlyStateMachine : StateMachineBase
    {
        public override async Task RunAsync()
        {
            switch (Stage)
            {
                case "Start":
                    await SetStageAsync("Start");
                    // 开始部署编译Actor
                    await DeployAndRunActorAsync(new RunActorParam("CompileActor", InitialBlobs.Where(x => x.Name.StartsWith("Main"))));
                    break;
            }
        }
    }
}
