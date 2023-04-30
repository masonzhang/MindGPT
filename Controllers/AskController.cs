using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ElectronNET.API;
using ElectronNET.WebApp.Utilities;
using ElectronNET.WebApp.Utils;
using Microsoft.AspNetCore.Mvc;
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
using NuGet.DependencyResolver;

namespace ElectronNET.WebApp.Controllers;

public class AskController: Controller
{
    public async Task<IActionResult> Index()
    {
        // configure Semantic Kernel
        var kernel = Kernel.Builder.Configure(c =>
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

        kernel.ImportSemanticSkillFromDirectory(RepoFiles.SkillsPath(), "CalendarSkill");
        kernel.ImportSemanticSkillFromDirectory(RepoFiles.SkillsPath(), "ChatSkill");
        kernel.ImportSemanticSkillFromDirectory(RepoFiles.SkillsPath(), "ChildrensBookSkill");
        kernel.ImportSemanticSkillFromDirectory(RepoFiles.SkillsPath(), "ClassificationSkill");
        kernel.ImportSemanticSkillFromDirectory(RepoFiles.SkillsPath(), "CodingSkill");
        kernel.ImportSemanticSkillFromDirectory(RepoFiles.SkillsPath(), "FunSkill");
        kernel.ImportSemanticSkillFromDirectory(RepoFiles.SkillsPath(), "IntentDetectionSkill");
        kernel.ImportSemanticSkillFromDirectory(RepoFiles.SkillsPath(), "MiscSkill");
        kernel.ImportSemanticSkillFromDirectory(RepoFiles.SkillsPath(), "QASkill");
        kernel.ImportSemanticSkillFromDirectory(RepoFiles.SkillsPath(), "SummarizeSkill");
        kernel.ImportSemanticSkillFromDirectory(RepoFiles.SkillsPath(), "WriterSkill");

        kernel.ImportSkill(new EmailSkill(new OutlookMailConnector(new GraphServiceClient(new HttpClient()))), "email");
        kernel.ImportSkill(new TextSkill(), "text");

        Console.WriteLine($"HybridSupport.IsElectronActive: {HybridSupport.IsElectronActive}");
        if (HybridSupport.IsElectronActive)
        {
            async void AskListener(object goal)
            {
                var goalString = (goal.ToString() ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(goalString))
                {
                    return;
                }
                
                var plan = await new SequentialPlanner(kernel).CreatePlanAsync(goal.ToString() ?? string.Empty);
                await RepoFiles.ExecutePlanAsync(kernel, plan, p => Send("got-ask-progress", RepoFiles.PlanToMermaid(p)));
                
                Send("got-ask");
            }

            await Electron.IpcMain.On("ask", AskListener);
        }
        else
        {
            // var plan = new SequentialPlanner(kernel).CreatePlanAsync("send email to Wesley about what is vector database").Result;
            // var plan = new SequentialPlanner(kernel).CreatePlanAsync("create an introduction about vector database, write a local text file and then open the file with notepad").Result;
            // var plan = new SequentialPlanner(kernel).CreatePlanAsync("Create a book with 3 chapters about a group of kids in a club called 'The Thinking Caps.'").Result;
            // var plan = new SequentialPlanner(kernel).CreatePlanAsync("create an introduction about Microsoft, save a local text file and then send to Wesley").Result;
            // return new JsonResult(DumpPlan(await ExecutePlanAsync(kernel, plan)));
        }

        return new JsonResult("");
    }

    private static void Send(string evt, params object[] data)
    {
        var mainWindow = Electron.WindowManager.BrowserWindows.First();
        Electron.IpcMain.Send(mainWindow, evt , data);
    }

    private static Dictionary<string, object> DumpPlan(Plan plan)
    {
        var result = new Dictionary<string, object>
        {
            {"name", plan.Name},
            {"desc", plan.Description},
            {"steps", plan.Steps.Select(DumpPlan).ToList()}
        };

        return result;
    }
}