using JoyOI.ManagementService.Core;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Migrations;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace StateMachineAndActor.Tyvj
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

    public class TyvjJudgeStateMachine : StateMachineBase
    {
        public static Regex InputFileRegex = new Regex("input_[0-9]{0,}.txt");
        public static Regex OutputFileRegex = new Regex("output_[0-9]{0,}.txt");

        public override async Task RunAsync()
        {
            switch (Stage)
            {
                case "Start":
                    await SetStageAsync("Start");
                    // 开始部署编译Actor
                    await DeployAndRunActorAsync(new RunActorParam("TyvjCompileActor", InitialBlobs.Where(x => x.Name.StartsWith("Main"))));
                    goto case "RunUserProgram";
                case "RunUserProgram":
                    await SetStageAsync("RunUserProgram");

                    // 获取编译Actor的运行结果
                    var compileRunnerResult = await StartedActors
                        .FindSingleActor("Start", "TyvjCompileActor")
                        .Outputs.FindSingleBlob("runner.json")
                        .ReadAsJsonAsync<RunnerReturn>(this);

                    // 如果程序返回值不为0
                    if (compileRunnerResult.ExitCode != 0)
                    {
                        if (compileRunnerResult.IsTimeout) // 超时
                        {
                            await HttpInvokeAsync(HttpMethod.Post, "/JoyOI/Callback", new ManagementServiceCallBack
                            {
                                StateMachineId = Id,
                                Status = "Compile Error",
                                Hint = "Compiler timeout. " + compileRunnerResult.TotalTime + "ms"
                            });
                        }
                        else // 其他编译异常
                        {
                            await HttpInvokeAsync(HttpMethod.Post, "/JoyOI/Callback", new ManagementServiceCallBack
                            {
                                StateMachineId = Id,
                                Status = "Compile Error",
                                Hint = await StartedActors
                                    .FindSingleActor("Start", "TyvjCompileActor")
                                    .Outputs
                                    .FindSingleBlob("stderr.txt")
                                    .ReadAllTextAsync(this) 
                                    + await StartedActors
                                    .FindSingleActor("Start", "TyvjCompileActor")
                                    .Outputs
                                    .FindSingleBlob("stdout.txt")
                                    .ReadAllTextAsync(this)
                            });
                        }
                        break; // 终止状态机
                    }
                    else // 编译通过，部署运行选手程序的Actor
                    {
                        var runActorParams = new List<RunActorParam>();
                        var inputs = InitialBlobs.Where(x => InputFileRegex.IsMatch(x.Name));
                        var userProgram = StartedActors.FindSingleActor("Start", "TyvjCompileActor").Outputs.FindSingleBlob("Main.out"); // 找到用户程序
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
                    var deployments = new List<RunActorParam>();
                    foreach (var x in RunUserPrograms)
                    {
                        var json4 = await x.Outputs.FindSingleBlob("runner.json").ReadAsJsonAsync<RunnerReturn>(this);
                        if (json4.PeakMemory > 134217728) // 判断是否超出内存限制
                        {
                            tasks4.Add(HttpInvokeAsync(HttpMethod.Post, "/JoyOI/Callback", new ManagementServiceCallBack
                            {
                                StateMachineId = Id,
                                Status = "Memory Limit Exceeded",
                                Time = json4.UserTime,
                                Memory = json4.PeakMemory,
                                InputBlobId = x.Inputs.FindSingleBlob("stdin.txt").Id
                            }));
                        }
                        else if (json4.IsTimeout)// 判断是否超时
                        {
                            tasks4.Add(HttpInvokeAsync(HttpMethod.Post, "/JoyOI/Callback", new ManagementServiceCallBack
                            {
                                StateMachineId = Id,
                                Status = "Time Limit Exceeded",
                                Time = json4.TotalTime,
                                Memory = json4.PeakMemory,
                                InputBlobId = x.Inputs.FindSingleBlob("stdin.txt").Id
                            }));
                        }
                        else if (json4.ExitCode != 0) // 判断是否运行时错误
                        {
                            tasks4.Add(HttpInvokeAsync(HttpMethod.Post, "/JoyOI/Callback", new ManagementServiceCallBack
                            {
                                StateMachineId = Id,
                                Status = "Runtime Error",
                                Time = json4.UserTime,
                                Memory = json4.PeakMemory,
                                Hint = json4.Error,
                                InputBlobId = x.Inputs.FindSingleBlob("stdin.txt").Id
                            }));
                        }
                        else // 如果运行没有失败，则部署Validator
                        {
                            var answerFilename = InitialBlobs.Single(y => y.Id == x.Inputs.FindSingleBlob("stdin.txt").Id && InputFileRegex.IsMatch(y.Name)).Name.Replace("input_", "output_");
                            var answer = InitialBlobs.FindSingleBlob(answerFilename);
                            var stdout = x.Outputs.Single(y => y.Name == "stdout.txt");
                            var validator = InitialBlobs.FindSingleBlob("Validator.out");
                            deployments.Add(new RunActorParam("TyvjCompareActor", new[]
                            {
                                new BlobInfo(answer.Id, "std.txt", x.Inputs.FindSingleBlob("stdin.txt").Id.ToString()),
                                new BlobInfo(stdout.Id, "out.txt"),
                                validator
                            }));
                        }
                    }
                    tasks4.Add(DeployAndRunActorsAsync(deployments.ToArray()));
                    await Task.WhenAll(tasks4);
                    goto case "Finally";
                case "Finally":
                    await SetStageAsync("Finally");
                    var compareActors = StartedActors.FindActor("ValidateUserOutput", "TyvjCompareActor").ToList();
                    var tasks5 = new List<Task>();
                    foreach (var x in compareActors)
                    {
                        var json5 = await x.Outputs.FindSingleBlob("runner.json").ReadAsJsonAsync<RunnerReturn>(this);

                        tasks5.Add(HttpInvokeAsync(HttpMethod.Post, "/JoyOI/Callback", new ManagementServiceCallBack
                        {
                            StateMachineId = Id,
                            Status = json5.ExitCode == 0 ? "Accepted" : (json5.ExitCode == 2 ? "Wrong Answer" : (json5.ExitCode == 1 ? "Presentation Error" : "Validator Error")),
                            Time = json5.UserTime,
                            Memory = json5.PeakMemory,
                            InputBlobId = Guid.Parse(x.Inputs.FindSingleBlob("std.txt").Tag),
                            Hint = await x.Outputs.FindSingleBlob("stdout.txt").ReadAllTextAsync(this)
                        }));
                    }
                    await Task.WhenAll(tasks5);
                    break;
            }
        }

        public override async Task HandleErrorAsync(Exception ex)
        {
            await HttpInvokeAsync(HttpMethod.Post, "/JoyOI/Callback", new ManagementServiceCallBack
            {
                StateMachineId = Id,
                Status = "System Error",
                Hint = ex.ToString()
            });
            await base.HandleErrorAsync(ex);
        }
    }
}
