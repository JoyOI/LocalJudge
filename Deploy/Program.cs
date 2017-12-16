using System;
using System.IO;
using System.Threading;
using Microsoft.EntityFrameworkCore.Migrations;
using JoyOI.ManagementService.SDK;

/*
{
    "code": 200,
    "msg": null,
    "data": [
        {
            "id": "0083c7bd-7c14-1035-82ec-54eca0c82300",
            "body": null,
            "timeStamp": 1500295059,
            "remark": "Validator.out"
        },
        {
            "id": "0083c826-167a-103e-79f2-0f3b339d9812",
            "body": null,
            "timeStamp": 1500339986,
            "remark": "Main.cpp"
        },
        {
            "id": "0083c826-167c-1015-0977-4c03a3a025d2",
            "body": null,
            "timeStamp": 1500339986,
            "remark": "output_01.txt"
        },
        {
            "id": "0083c826-167e-1033-25ea-afea579542d8",
            "body": null,
            "timeStamp": 1500339986,
            "remark": "output_02.txt"
        },
        {
            "id": "0083c826-167e-100f-505b-dc50f61912bc",
            "body": null,
            "timeStamp": 1500339986,
            "remark": "input_02.txt"
        },
        {
            "id": "0083c826-16b7-1024-751b-652bf16b00f0",
            "body": null,
            "timeStamp": 1500339986,
            "remark": "input_01.txt"
        }
    ]
}
*/

namespace Deploy
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new ManagementServiceClient("https://mgmtsvc.1234.sh", @"C:\Users\Yuko\Documents\webapi-client.pfx", "123456");
            client.PatchStateMachineDefinitionAsync("CompileOnlyStateMachine", File.ReadAllText(@"C:\Users\Yuko\Documents\GitHub\LocalJudge\StateMachineAndActor\CompileOnlyStateMachine.cs"), null).Wait();
            client.PatchStateMachineDefinitionAsync("JudgeStateMachine", File.ReadAllText(@"C:\Users\Yuko\Documents\GitHub\LocalJudge\StateMachineAndActor\JudgeStateMachine.cs"), null).Wait();
            client.PatchStateMachineDefinitionAsync("HackStateMachine", File.ReadAllText(@"C:\Users\Yuko\Documents\GitHub\LocalJudge\StateMachineAndActor\Hack\HackStateMachine.cs"), null).Wait();
            client.PatchActorAsync("CompareActor", File.ReadAllText(@"C:\Users\Yuko\Documents\GitHub\LocalJudge\StateMachineAndActor\CompareActor.cs")).Wait();
            client.PatchActorAsync("RunUserProgramActor", File.ReadAllText(@"C:\Users\Yuko\Documents\GitHub\LocalJudge\StateMachineAndActor\RunUserProgramActor.cs")).Wait();
            client.PatchActorAsync("CompileActor", File.ReadAllText(@"C:\Users\Yuko\Documents\GitHub\LocalJudge\StateMachineAndActor\CompileActor.cs"), default(CancellationToken)).Wait();
            client.PatchActorAsync("HackActor", File.ReadAllText(@"C:\Users\Yuko\Documents\GitHub\LocalJudge\StateMachineAndActor\Hack\HackRunActor.cs"), default(CancellationToken)).Wait();

            //var validatorId = client.PutBlobAsync("Validator.out", File.ReadAllBytes(@"C:\Users\Yuko\Documents\Validator.out")).Result;
            //Console.WriteLine(validatorId);
            Console.Read();
        }
    }
}
