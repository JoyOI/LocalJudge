using System;
using System.IO;
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
            //client.PutStateMachineDefinitionAsync("TyvjJudgeStateMachine", File.ReadAllText(@"C:\Users\Yuko\Documents\GitHub\StateMachineAndActor\StateMachineAndActor\Tyvj\TyvjJudgeStateMachine.cs"), null).Wait();
            //client.PatchActorAsync("TyvjCompareActor", File.ReadAllText(@"C:\Users\Yuko\Documents\GitHub\StateMachineAndActor\StateMachineAndActor\Tyvj\TyvjCompareActor.cs"), default(CancellationToken));
            //client.PatchActorAsync("TyvjCompileActor", File.ReadAllText(@"C:\Users\Yuko\Documents\GitHub\StateMachineAndActor\StateMachineAndActor\Tyvj\TyvjCompareActor.cs"), default(CancellationToken));
            //client.PatchActorAsync("TyvjRunUserProgramActor", File.ReadAllText(@"C:\Users\Yuko\Documents\GitHub\StateMachineAndActor\StateMachineAndActor\Tyvj\TyvjRunUserProgramActor.cs"), default(CancellationToken));
            //client.PutBlobAsync("Validator.out", File.ReadAllBytes(@"C:\Users\Yuko\Documents\Validator.out"));
            //client.PutBlobAsync("Main.cpp", File.ReadAllBytes(@"C:\Users\Yuko\Documents\Main.cpp"));
            //client.PutBlobAsync("input_01.txt", File.ReadAllBytes(@"C:\Users\Yuko\Documents\input_01.txt"));
            //client.PutBlobAsync("input_02.txt", File.ReadAllBytes(@"C:\Users\Yuko\Documents\input_02.txt"));
            //client.PutBlobAsync("output_01.txt", File.ReadAllBytes(@"C:\Users\Yuko\Documents\output_01.txt"));
            //client.PutBlobAsync("output_02.txt", File.ReadAllBytes(@"C:\Users\Yuko\Documents\output_02.txt"));

            //var x = client.GetAllActorsAsync().Result;
            //Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(x));

            var x = client.PutStateMachineInstanceAsync("TyvjJudgeStateMachine", "http://tyvj.cn", new[]
            {
                new BlobInfo { Id = Guid.Parse("0083c7bd-7c18-101c-fc5d-bca53aefdc5f"), Name = "Validator.cs" },
                new BlobInfo { Id = Guid.Parse("0083c826-167a-103e-79f2-0f3b339d9812"), Name = "Main.cpp" },
                new BlobInfo { Id = Guid.Parse("0083c826-167c-1015-0977-4c03a3a025d2"), Name = "output_01.txt" },
                new BlobInfo { Id = Guid.Parse("0083c826-167e-1033-25ea-afea579542d8"), Name = "output_02.txt" },
                new BlobInfo { Id = Guid.Parse("0083c826-167e-100f-505b-dc50f61912bc"), Name = "input_02.txt" },
                new BlobInfo { Id = Guid.Parse("0083c826-16b7-1024-751b-652bf16b00f0"), Name = "input_01.txt" },
            }).Result;
            Console.WriteLine("Finished " + x);

            Console.Read();
        }
    }
}
