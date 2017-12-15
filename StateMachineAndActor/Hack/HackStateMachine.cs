using JoyOI.ManagementService.Core;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Migrations;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http;
using System;

namespace StateMachineAndActor.Hack
{
    public class HackStateMachine : StateMachineBase
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

        public override async Task RunAsync()
        {
            switch (Stage)
            {
                case "Start":
                    await SetStageAsync("Start");
                    goto case "ValidateData";
                case "ValidateData":
                    await SetStageAsync("ValidateData");
                    if (InitialBlobs.Any(x => x.Name.StartsWith("Range")))
                    {
                        var originRange = InitialBlobs.First(x => x.Name.StartsWith("Range"));
                        var rangeBlob = new BlobInfo(originRange.Id, originRange.Name.Replace("Range", "Main"));
                        var originData = InitialBlobs.FindSingleBlob("data.txt");
                        var dataBlob = new BlobInfo(originData.Id, "stdin.txt");
                        await DeployAndRunActorAsync(new RunActorParam("HackRunActor", new BlobInfo[] { rangeBlob, dataBlob }, "ValidateData"));
                        var validateActor = StartedActors.FindSingleActor(actor: "HackRunActor");
                        var validateResult = await validateActor
                             .Outputs.FindSingleBlob("runner.json")
                             .ReadAsJsonAsync<RunnerReturn>(this);
                        if (validateResult.ExitCode != 0)
                        {
                            goto case "Finally";
                        }
                    }
                    goto case "GenerateStandardAnswer";
                case "GenerateStandardAnswer": // TODO: Limit time & mem
                    await SetStageAsync("GenerateStandardAnswer");
                    var originStandard = InitialBlobs.First(x => x.Name.StartsWith("Standard"));
                    var standardBlob = new BlobInfo(originStandard.Id, originStandard.Name.Replace("Standard", "Main"));
                    var originData2 = InitialBlobs.FindSingleBlob("data.txt");
                    var dataBlob2 = new BlobInfo(originData2.Id, "stdin.txt");
                    await DeployAndRunActorAsync(new RunActorParam("HackRunActor", new BlobInfo[] { standardBlob, dataBlob2 }, "GenerateStandardAnswer"));
                    var standardActor = StartedActors.Last(x => x.Tag == "GenerateStandardAnswer");
                    var standardResult = await standardActor
                         .Outputs.FindSingleBlob("runner.json")
                         .ReadAsJsonAsync<RunnerReturn>(this);
                    if (standardResult.ExitCode != 0)
                    {
                        goto case "Finally";
                    }
                    goto case "GenerateUserAnswer";
                case "GenerateUserAnswer":
                    await SetStageAsync("GenerateUserAnswer");
                    var originHackee = InitialBlobs.First(x => x.Name.StartsWith("Hackee"));
                    var hackeeBlob = new BlobInfo(originHackee.Id, originHackee.Name.Replace("Hackee", "Main"));
                    var originData3 = InitialBlobs.FindSingleBlob("data.txt");
                    var dataBlob3 = new BlobInfo(originData3.Id, "stdin.txt");
                    await DeployAndRunActorAsync(new RunActorParam("HackRunActor", new BlobInfo[] { hackeeBlob, dataBlob3 }, "GenerateUserAnswer"));
                    var hackeeActor = StartedActors.Last(x => x.Tag == "GenerateUserAnswer");
                    var hackeeResult = await hackeeActor
                         .Outputs.FindSingleBlob("runner.json")
                         .ReadAsJsonAsync<RunnerReturn>(this);
                    if (hackeeResult.ExitCode != 0)
                    {
                        goto case "Finally";
                    }
                    goto case "ValidateAnswer";
                case "ValidateAnswer":
                    await SetStageAsync("ValidateAnswer");
                    var validatorBlob = InitialBlobs.First(x => x.Name.StartsWith("Validator"));
                    var hackeeActor2 = StartedActors.Last(x => x.Tag == "GenerateUserAnswer");
                    var originHackeeOutput = hackeeActor2
                         .Outputs.FindSingleBlob("stdout.txt");
                    var hackeeOutput = new BlobInfo(originHackeeOutput.Id, "out.txt");

                    var standardActor2 = StartedActors.Last(x => x.Tag == "GenerateStandardAnswer");
                    var originStandard2 = standardActor2.FindSingleOutputBlob("stdout.txt");
                    var standardBlob2 = new BlobInfo(originStandard2.Id, "std.txt");

                    await DeployAndRunActorAsync(new RunActorParam("CompareActor", new BlobInfo[] { validatorBlob, hackeeOutput, standardBlob2 }));
                    goto case "Finally";
                case "Finally":
                    await SetStageAsync("Finally");
                    await HttpInvokeAsync(HttpMethod.Post, "/management/hack/stagechange/" + this.Id, null);
                    break;
            }
        }

        public override Task HandleErrorAsync(Exception ex)
        {
            HttpInvokeAsync(HttpMethod.Post, "/management/hack/stagechange/" + this.Id + "?se=true", null);
            return base.HandleErrorAsync(ex);
        }
    }
}
