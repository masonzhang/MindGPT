using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ElectronNET.WebApp.Utilities;
using ElectronNET.WebApp.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.CoreSkills;
using Microsoft.SemanticKernel.KernelExtensions;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Planning.Planners;
using Microsoft.SemanticKernel.Reliability;
using Microsoft.SemanticKernel.Skills.MsGraph;
using Microsoft.SemanticKernel.Skills.MsGraph.Connectors;
namespace ElectronNET.WebApp.Ask;
using Microsoft.AspNetCore.SignalR;

public class AskHub : Hub
{
    private readonly IKernel _kernel;

    public AskHub()
    {
        // configure Semantic Kernel
        _kernel = Kernel.Builder.Configure(c =>
            {
                // Configure AI backend used by the kernel
                var (azureEndpoint, apiKey) = (Env.Var("MIND_AZURE_ENDPOINT"), Env.Var("MIND_AZURE_OPENAI_KEY"));

                c.AddAzureChatCompletionService("davinci", Env.Var("MIND_AZURE_MODEL_COMPLETION"), azureEndpoint, apiKey!);
                c.AddAzureTextEmbeddingGenerationService("ada", Env.Var("MIND_AZURE_MODEL_EMBEDDING"), azureEndpoint, apiKey!);

                c.SetDefaultHttpRetryConfig(new HttpRetryConfig
                {
                    MaxRetryCount = 3,
                    UseExponentialBackoff = true,
                    //  MinRetryDelay = TimeSpan.FromSeconds(2),
                    //  MaxRetryDelay = TimeSpan.FromSeconds(8),
                    //  MaxTotalRetryTime = TimeSpan.FromSeconds(30),
                    //  RetryableStatusCodes = new[] { HttpStatusCode.TooManyRequests, HttpStatusCode.RequestTimeout },
                    //  RetryableExceptions = new[] { typeof(HttpRequestException) }
                });
            })
            .WithMemoryStorage(new VolatileMemoryStore())
            .WithLogger(LoggerFactory.Create(o => o.AddConsole()).CreateLogger("SemanticKernel"))
            .Build();

        // _kernel.ImportSemanticSkillFromDirectory(RepoFiles.SkillsPath(), "CalendarSkill");
        // _kernel.ImportSemanticSkillFromDirectory(RepoFiles.SkillsPath(), "ChatSkill");
        _kernel.ImportSemanticSkillFromDirectory(RepoFiles.SkillsPath(), "ChildrensBookSkill");
        // _kernel.ImportSemanticSkillFromDirectory(RepoFiles.SkillsPath(), "ClassificationSkill");
        // _kernel.ImportSemanticSkillFromDirectory(RepoFiles.SkillsPath(), "CodingSkill");
        // _kernel.ImportSemanticSkillFromDirectory(RepoFiles.SkillsPath(), "FunSkill");
        _kernel.ImportSemanticSkillFromDirectory(RepoFiles.SkillsPath(), "IntentDetectionSkill");
        _kernel.ImportSemanticSkillFromDirectory(RepoFiles.SkillsPath(), "MiscSkill");
        _kernel.ImportSemanticSkillFromDirectory(RepoFiles.SkillsPath(), "QASkill");
        _kernel.ImportSemanticSkillFromDirectory(RepoFiles.SkillsPath(), "SummarizeSkill");
        _kernel.ImportSemanticSkillFromDirectory(RepoFiles.SkillsPath(), "WriterSkill");

        // _kernel.ImportSkill(new EmailSkill(new OutlookMailConnector(new GraphServiceClient(new HttpClient()))), "email");
        _kernel.ImportSkill(new TextSkill(), "text");
    }
    
    private void ProgressUpdate(Plan p)
    {
        Clients.All.SendAsync("OnGoalProgress", RepoFiles.PlanToMermaid(p), string.Join("\n", p.State.Select(s => $"{s.Key}: {s.Value}")));
    }
    
    private void SpinTipUpdate(Plan p)
    {
        Clients.All.SendAsync("OnSpinTip", p == null ? string.Empty : string.Join(".\n", p.NamedParameters.Select(s => $"{s.Key}: {s.Value}")));
    }
    
    public async Task SetGoal(string goal)
    {
        if (string.IsNullOrEmpty(goal))
        {
            return;
        }

        var plan = await new SequentialPlanner(_kernel).CreatePlanAsync(goal);
        ProgressUpdate(plan);
        SpinTipUpdate(plan);
        
        await RepoFiles.ExecutePlanAsync(_kernel, plan, ProgressUpdate, SpinTipUpdate);
    }
}