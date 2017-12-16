using JoyOI.ManagementService.Core;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Migrations;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http;
using System.IO;
using System;

namespace StateMachineAndActor.Hack
{
    public class HackStateMachine : StateMachineBase
    {
        #region File Structures
        public class Limitations
        {
            public int UserTime { get; set; }
            public int PhysicalTime { get; set; }
            public int Memory { get; set; }
        }

        public class RunnerReturn
        {
            public int UserTime { get; set; }
            public int TotalTime { get; set; }
            public int ExitCode { get; set; }
            public int PeakMemory { get; set; }
            public bool IsTimeout { get; set; }
            public string Error { get; set; }
        }
        #endregion

        #region Data Range Validation
        private RunActorParam PrepareDataRangeValidatorRunParams()
        {
            if (!InitialBlobs.Any(x => x.Name.StartsWith("Range")))
            {
                return null;
            }

            var range = InitialBlobs.First(x => x.Name.StartsWith("Range"));
            var rangeBlob = new BlobInfo(range.Id, range.Name.Replace("Range", "Main"));
            var originData = InitialBlobs.FindSingleBlob("data.txt");
            var dataBlob = new BlobInfo(originData.Id, "stdin.txt");
            return new RunActorParam("HackRunActor", new BlobInfo[] 
            {
                rangeBlob,
                dataBlob,
                InitialBlobs.FindSingleBlob("limit.json")
            }, "ValidateData");
        }

        private async Task<bool> IsPassedDataRangeValidationAsync()
        {
            var meta = await InitialBlobs.FindSingleBlob("limit.json").ReadAsJsonAsync<Limitations>(this);
            var actor = StartedActors.FindSingleActor(actor: "HackRunActor");
            var runner = await actor
                 .Outputs.FindSingleBlob("runner.json")
                 .ReadAsJsonAsync<RunnerReturn>(this);
            return runner.ExitCode == 0 && !runner.IsTimeout;
        }
        #endregion

        #region Standard Program
        private IEnumerable<RunActorParam> PrepareMultiStandardProgramRunActorParams()
        {
            var standards = InitialBlobs.Where(x => x.Name.StartsWith("Standard"));
            var data = InitialBlobs.FindSingleBlob("data.txt");
            foreach (var x in standards)
            {
                yield return new RunActorParam("HackRunActor", new[]
                {
                    new BlobInfo(x.Id, "Main" + Path.GetExtension(x.Name)),
                    new BlobInfo(data.Id, "stdin.txt"),
                    InitialBlobs.FindSingleBlob("limit.json")
                }, "Standard");
            }
        }

        private async Task<bool> IsAllStandardSucceededAsync()
        {
            var meta = await InitialBlobs.FindSingleBlob("limit.json").ReadAsJsonAsync<Limitations>(this);

            var runners = StartedActors
                .Where(x => x.Tag == "Standard")
                .SelectMany(x => x.Outputs)
                .Where(x => x.Name == "runner.json")
                .ToList();

            foreach (var x in runners)
            {
                var runner = await x.ReadAsJsonAsync<RunnerReturn>(this);
                if (runner.ExitCode == 0 && !runner.IsTimeout && runner.PeakMemory <= meta.Memory)
                {
                    continue;
                }
                return false;
            }

            return true;
        }
        #endregion

        #region Hackee Program
        private IEnumerable<RunActorParam> PrepareMultiHackeeProgramRunParams()
        {
            var hackees = InitialBlobs.Where(x => x.Name.StartsWith("Hackee"));
            var data = InitialBlobs.FindSingleBlob("data.txt");

            foreach (var x in hackees)
            {
                yield return new RunActorParam("HackRunActor", new BlobInfo[] 
                {
                    new BlobInfo(x.Id, "Main" + Path.GetExtension(x.Name)),
                    new BlobInfo(data.Id, "stdin.txt"),
                    InitialBlobs.FindSingleBlob("limit.json")
                }, "Hackee=" + x.Tag);
            }
        }
        #endregion

        #region Answer Validation
        private async Task<IEnumerable<RunActorParam>> PrepareMultiAnswerValidatorRunParamsAsync()
        {
            var meta = await InitialBlobs.FindSingleBlob("limit.json").ReadAsJsonAsync<Limitations>(this);
            var ret = new List<RunActorParam>();
            var actors = StartedActors.Where(x => x.Tag.StartsWith("Hackee="));

            var answers = StartedActors
                .Where(x => x.Tag == "Standard")
                .SelectMany(x => x.Outputs)
                .Where(x => x.Name == "stdout.txt");

            foreach (var x in actors)
            {
                var runner = await x.Outputs
                    .FindSingleBlob("runner.json")
                    .ReadAsJsonAsync<RunnerReturn>(this);

                var validator = InitialBlobs.Single(y => y.Name.StartsWith("Validator"));

                if (runner.ExitCode == 0 && !runner.IsTimeout && runner.PeakMemory <= meta.Memory)
                {
                    foreach (var y in answers)
                    {
                        var output = x.Outputs.FindSingleBlob("stdout.txt");
                        ret.Add(new RunActorParam("HackRunActor", new BlobInfo[] 
                        {
                            new BlobInfo(output.Id, "out.txt"),
                            new BlobInfo(y.Id, "std.txt"),
                            validator,
                            InitialBlobs.FindSingleBlob("limit.json")
                        }, x.Tag));
                    }
                }
            }

            return ret;
        }
        #endregion

        #region Main
        public override async Task RunAsync()
        {
            switch (Stage)
            {
                case "Start":
                    await SetStageAsync("Start");
                    goto case "ValidateData";
                case "ValidateData":
                    await SetStageAsync("ValidateData");
                    var range = PrepareDataRangeValidatorRunParams();
                    if (range != null)
                    {
                        await DeployAndRunActorAsync(range);
                        if (!await IsPassedDataRangeValidationAsync())
                        {
                            goto case "Finally";
                        }
                    }
                    goto case "GenerateStandardAnswer";
                case "GenerateStandardAnswer":
                    await SetStageAsync("GenerateStandardAnswer"); 
                    await DeployAndRunActorsAsync(PrepareMultiStandardProgramRunActorParams().ToArray());
                    if (!await IsAllStandardSucceededAsync())
                    {
                        goto case "Finally";
                    }
                    goto case "GenerateHackeeAnswer";
                case "GenerateHackeeAnswer":
                    await SetStageAsync("GenerateHackeeAnswer");
                    await DeployAndRunActorsAsync(PrepareMultiHackeeProgramRunParams().ToArray());
                    goto case "ValidateAnswer";
                case "ValidateAnswer":
                    await SetStageAsync("ValidateAnswer");
                    await PrepareMultiAnswerValidatorRunParamsAsync();
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
        #endregion
    }
}
