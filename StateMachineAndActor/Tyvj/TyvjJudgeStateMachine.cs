using JoyOI.ManagementService.Core;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Migrations;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;

namespace StateMachineAndActor.Tyvj
{
    public class RunnerReturn
    {
        public int UserTime { get; set; }
        public int TotalTime { get; set; }
        public int ExitCode { get; set; }
        public int PeakMemory { get; set; }
        public bool IsTimeout { get; set; }
    }

    public class TyvjCppJudgeStateMachine : StateMachineBase
    {
        public static Regex SourceCodeRegex = new Regex("Main.[a-zA-Z]{1,5}");
        public static Regex InputFileRegex = new Regex("input_[0-9]{0,}.txt");
        public static Regex OutputFileRegex = new Regex("output_[0-9]{0,}.txt");

        public override async Task RunAsync()
        {
            switch (Stage)
            {
                case "Start":
                    await SetStageAsync("Start");
                    // 开始部署编译Actor
                    await DeployAndRunActorAsync(new RunActorParam("TyvjCompileActor", InitialBlobs.Where(x => SourceCodeRegex.IsMatch(x.Name))));
                    goto case "RunUserProgram";
                case "RunUserProgram":
                    await SetStageAsync("RunUserProgram");

                    // 获取编译Actor的运行结果
                    var compileRunnerResult = await StartedActors
                        .FindSingleActor("Start", "TyvjCompileActor")
                        .Outputs.FindBlob("runner.json")
                        .ReadAsJsonAsync<RunnerReturn>(this);

                    // 如果程序返回值不为0
                    if (compileRunnerResult.ExitCode != 0)
                    {
                        if (compileRunnerResult.IsTimeout) // 超时
                        {
                            //await HttpInvokeAsync(HttpMethod.Post, "/JudgeResult/" + this.Id, new
                            //{
                            //    Result = "Compile Error",
                            //    Error = "Compiler timeout.",
                            //    TimeUsed = compileRunnerResult.UserTime
                            //});
                        }
                        else // 其他编译异常
                        {
                            //await HttpInvokeAsync(HttpMethod.Post , "/JudgeResult/" + this.Id, new
                            //{
                            //    Result = "Compile Error",
                            //    Error = StartedActors
                            //        .FindSingleActor("Start", "TyvjCompileActor")
                            //        .Outputs
                            //        .FindBlob("stderr.txt"),
                            //    ExitCode = compileRunnerResult.ExitCode
                            //});
                        }
                        break; // 终止状态机
                    }
                    else // 编译通过，部署运行选手程序的Actor
                    {
                        var runActorParams = new List<RunActorParam>();
                        var inputs = InitialBlobs.Where(x => InputFileRegex.IsMatch(x.Name));
                        var userProgram = StartedActors.FindSingleActor("Start", "TyvjCompileActor").Outputs.FindBlob("Main.out"); // 找到用户程序
                        foreach (var x in inputs)
                        {
                            runActorParams.Add(new RunActorParam("TyvjRunUserProgramActor", new BlobInfo(x.Id, "stdin.txt"), userProgram));
                        }
                        await DeployAndRunActorsAsync(runActorParams.ToArray());
                        goto case "ValidateUserOutput";
                    }
                case "ValidateUserOutput":
                    await SetStageAsync("ValidateUserOutput");
                    var RunUserPrograms = StartedActors.FindActor("RunUserProgram", "TyvjRunUserProgramActor").ToList(); // 获取运行用户程序Actors
                    var tasks4 = new List<Task>();
                    foreach (var x in RunUserPrograms)
                    {
                        var json4 = await x.Outputs.FindBlob("runner.json").ReadAsJsonAsync<RunnerReturn>(this);
                        if (json4.PeakMemory > 134217728) // 判断是否超出内存限制
                        {
                            //tasks4.Add(HttpInvokeAsync(HttpMethod.Post, "/JudgeResult/" + this.Id, new
                            //{
                            //    Result = "Memory Limit Exceeded",
                            //    InputFile = x.Inputs.Single(y => InputFileRegex.IsMatch(y.Name))
                            //}));
                        }
                        else if (json4.ExitCode != 0) // 判断是否运行时错误或超时
                        {
                            //tasks4.Add(HttpInvokeAsync(HttpMethod.Post, "/JudgeResult/" + this.Id, new
                            //{
                            //    Result = json4.IsTimeout ? "Time Limit Exceeded" : "Runtime Error",
                            //    InputFile = x.Inputs.Single(y => InputFileRegex.IsMatch(y.Name))
                            //}));
                        }
                        else // 如果运行没有失败，则部署Validator
                        {
                            var answerFilename = InitialBlobs.Single(y => y.Id == x.Inputs.FindBlob("stdin.txt").Id).Name.Replace("input_", "output_");
                            var answer = InitialBlobs.FindBlob(answerFilename);
                            var stdout = x.Outputs.Single(y => y.Name == "stdout.txt");
                            var validator = InitialBlobs.FindBlob("Validator.out");
                            tasks4.Add(DeployAndRunActorAsync(new RunActorParam("TyvjCompareActor", new[] 
                            {
                                new BlobInfo(answer.Id, "std.txt"),
                                new BlobInfo(stdout.Id, "out.txt"),
                                validator
                            })));
                        }
                    }
                    await Task.WhenAll(tasks4);
                    goto case "Finally";
                case "Finally":
                    await SetStageAsync("Finally");
                    var compareActors = StartedActors.FindActor("ValidateUserOutput", "TyvjCompareActor").ToList();
                    var tasks5 = new List<Task>();
                    foreach (var x in compareActors)
                    {
                        var json5 = await x.Outputs.FindBlob("runner.json").ReadAsJsonAsync<RunnerReturn>(this);
                        //tasks5.Add(HttpInvokeAsync(HttpMethod.Post, "/JudgeResult/" + Id, new
                        //{
                        //    Result = json5.ExitCode == 0 ? "Accepted" : (json5.ExitCode == 1 ? "Wrong Answer" : (json5.ExitCode == 2 ? "Presentation Error" : "Validator Error")),
                        //    TimeUsed = json5.UserTime,
                        //    MemoryUsed = json5.PeakMemory,
                        //    InputFile = x.Inputs.Single(y => OutputFileRegex.IsMatch(y.Name)).Name.Replace("output_", "input_")
                        //}));
                    }
                    await Task.WhenAll(tasks5);
                    break;
            }
        }
    }
}
