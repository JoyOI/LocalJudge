﻿using JoyOI.ManagementService.Core;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Migrations;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http;
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

    public class Limitations
    {
        public int UserTime { get; set; }

        public int PhysicalTime { get; set; }

        public int Memory { get; set; }
    }

    public class JudgeStateMachine : StateMachineBase
    {
        public static Regex InputFileRegex = new Regex("input_[0-9]{0,}.txt");
        public static Regex OutputFileRegex = new Regex("output_[0-9]{0,}.txt");

        public override async Task RunAsync()
        {
            if (InitialBlobs.Any(x => x.Name.EndsWith(".cs") || x.Name.EndsWith(".fs") || x.Name.EndsWith(".vb")))
            {
                this.Limitation.Memory = 1024 * 1024 * 1024;
                this.Limitation.Ulimit["nproc"] = 128;
            }

            switch (Stage)
            {
                case "Start":
                    await SetStageAsync("Start");
                    // 开始部署编译Actor
                    await DeployAndRunActorAsync(new RunActorParam("CompileActor", InitialBlobs.Where(x => x.Name.StartsWith("Main"))));
                    goto case "RunUserProgram";
                case "RunUserProgram":
                    await SetStageAsync("RunUserProgram");

                    // 获取编译Actor的运行结果
                    var compileRunnerResult = await StartedActors
                        .FindSingleActor("Start", "CompileActor")
                        .Outputs.FindSingleBlob("runner.json")
                        .ReadAsJsonAsync<RunnerReturn>(this);

                    // 如果程序返回值不为0
                    if (compileRunnerResult.ExitCode == 0)
                    {
                        var runActorParams = new List<RunActorParam>();
                        var inputs = InitialBlobs.Where(x => InputFileRegex.IsMatch(x.Name));
                        var userProgram = StartedActors.FindSingleActor("Start", "CompileActor").Outputs.Single(x => x.Name.StartsWith("Main.")); // 找到用户程序
                        var i = 0;
                        foreach (var x in inputs)
                        {
                            var blobs = new[]
                            {
                                new BlobInfo(x.Id, "stdin.txt"),
                                InitialBlobs.FindSingleBlob("limit.json"),
                                userProgram
                            };
                            runActorParams.Add(new RunActorParam(
                                "RunUserProgramActor",
                                blobs,
                                i++.ToString()));
                        }
                        await DeployAndRunActorsAsync(runActorParams.ToArray());
                        goto case "ValidateUserOutput";
                    }
                    else
                    {
                        goto case "Finally";
                    }
                case "ValidateUserOutput":
                    await SetStageAsync("ValidateUserOutput");
                    var RunUserPrograms = StartedActors.FindActor("RunUserProgram", "RunUserProgramActor").ToList(); // 获取运行用户程序Actors
                    var tasks4 = new List<Task>();
                    var deployments = new List<RunActorParam>();
                    var limit = await InitialBlobs.FindSingleBlob("limit.json").ReadAsJsonAsync<Limitations>(this);
                    foreach (var x in RunUserPrograms)
                    {
                        var json4 = await x.Outputs.FindSingleBlob("runner.json").ReadAsJsonAsync<RunnerReturn>(this);
                        if (json4.PeakMemory > limit.Memory) // 判断是否超出内存限制
                        {
                            continue;
                        }
                        else if (json4.IsTimeout)// 判断是否超时
                        {
                            continue;
                        }
                        else if (json4.ExitCode != 0) // 判断是否运行时错误
                        {
                            continue;
                        }
                        else // 如果运行没有失败，则部署Validator
                        {
                            var answerFilename = InitialBlobs.First(y => y.Id == x.Inputs.FindSingleBlob("stdin.txt").Id && InputFileRegex.IsMatch(y.Name)).Name.Replace("input_", "output_");
                            var answer = InitialBlobs.FindSingleBlob(answerFilename);
                            var stdout = x.Outputs.First(y => y.Name == "stdout.txt");
                            var validator = InitialBlobs.First(y => y.Name.StartsWith("Validator"));
                            deployments.Add(new RunActorParam("CompareActor", new[]
                            {
                                new BlobInfo(answer.Id, "std.txt", x.Inputs.FindSingleBlob("stdin.txt").Tag),
                                new BlobInfo(stdout.Id, "out.txt", x.Inputs.FindSingleBlob("stdin.txt").Tag),
                                validator
                            }, 
                            x.Tag));
                        }
                    }
                    tasks4.Add(DeployAndRunActorsAsync(deployments.ToArray()));
                    await Task.WhenAll(tasks4);
                    goto case "Finally";
                case "Finally":
                    await SetStageAsync("Finally");
                    HttpInvokeAsync(HttpMethod.Post, "/management/judge/stagechange/" + this.Id, null);
                    break;
            }
        }

        public override Task HandleErrorAsync(Exception ex)
        {
            HttpInvokeAsync(HttpMethod.Post, "/management/judge/stagechange/" + this.Id + "?se=true", null);
            return base.HandleErrorAsync(ex);
        }
    }
}
