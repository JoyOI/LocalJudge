using JoyOI.ManagementService.Core;
using Microsoft.EntityFrameworkCore.Migrations;
using System.Threading.Tasks;

namespace StateMachineAndActor.Test
{
    public class EmptyStateMachine : StateMachineBase
    {
        public override async Task RunAsync()
        {
            switch (Stage)
            {
                case "Start":
                    await SetStageAsync("Start");
                    // 开始部署Empty Actor
                    await DeployAndRunActorAsync(new RunActorParam("EmptyActor"));
                    goto case "RunSecond";
                case "RunSecond":
                    await SetStageAsync("RunSecond");
                    // 开始部署Empty Actor
                    await DeployAndRunActorAsync(new RunActorParam("EmptyActor"));
                    goto case "RunThird";
                case "RunThird":
                    await SetStageAsync("RunThird");
                    // 开始部署Empty Actor
                    await DeployAndRunActorAsync(new RunActorParam("EmptyActor"));
                    break;
            }
        }
    }
}
