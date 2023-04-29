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
                Send("got-ask-progress", PlanToMermaid(plan));
                await ExecutePlanAsync(kernel, plan);
                
                Send("got-ask");
            }

            await Electron.IpcMain.On("ask", AskListener);
        }
        else
        {
            // var plan = new SequentialPlanner(kernel).CreatePlanAsync("send email to Wesley about what is vector database").Result;
            // var plan = new SequentialPlanner(kernel).CreatePlanAsync("create an introduction about vector database, write a local text file and then open the file with notepad").Result;
            // var plan = new SequentialPlanner(kernel).CreatePlanAsync("Create a book with 3 chapters about a group of kids in a club called 'The Thinking Caps.'").Result;
            var plan = new SequentialPlanner(kernel).CreatePlanAsync("create an introduction about Microsoft, save a local text file and then send to Wesley").Result;
            return new JsonResult(DumpPlan(await ExecutePlanAsync(kernel, plan)));
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

    private static string PlanToMermaid(Plan plan)
    {
        var nameList = new List<string>();
        var relationshipList = new List<string>();
        var queue = new Queue<(int, Plan)>();
        queue.Enqueue((0, plan));

        while (queue.Count > 0)
        {
            var (sequence, step) = queue.Dequeue();
            var desc = !string.IsNullOrEmpty(step.Description) && step.Description != step.Name ? $": {step.Description}" : string.Empty;
            nameList.Add($"seq{sequence}(\"{sequence}. {step.Name}{desc}\")");

            for (int i = 0; i < step.Steps.Count; i++)
            {
                queue.Enqueue((sequence + i + 1, step.Steps[i]));
                relationshipList.Add($"seq{sequence} --> seq{sequence + i + 1}");
                if (step.NextStepIndex > 0)
                {
                    relationshipList.Add($"class seq{sequence} progress");
                }
                else if(step.NextStepIndex < i)
                {
                    relationshipList.Add($"class seq{sequence} completed");
                }
                else if(step.NextStepIndex == i)
                {
                    relationshipList.Add($"class seq{sequence} progress");
                }
                else
                {
                    relationshipList.Add($"class seq{sequence} pending");
                }
            }
        }

        // add mermaid header
        nameList.Insert(0, "```mermaid");
        nameList.Insert(1, "graph LR");
        nameList.Insert(2, "classDef completed fill:#f9f,stroke:#333,stroke-width:4px;");
        nameList.Insert(3, "classDef pending fill:#fff,stroke:#333,stroke-width:4px;");
        nameList.Insert(4, "classDef failed fill:#f00,stroke:#333,stroke-width:4px;");
        nameList.Insert(4, "classDef progress fill:#0f0,stroke:#333,stroke-width:4px;");
        relationshipList.Add("```");
        relationshipList.AddRange(plan.State.Select(s => $"{s.Key}: {s.Value}"));
        return string.Join("\n", nameList) + "\n" + string.Join("\n", relationshipList);
    }
    
    
    private static async Task<Plan> ExecutePlanAsync(
        IKernel kernel,
        Plan plan,
        string input = "",
        int maxSteps = 10)
    {
        Stopwatch sw = new();
        sw.Start();

        // loop until complete or at most N steps
        try
        {
            for (int step = 1; plan.HasNextStep && step < maxSteps; step++)
            {
                try
                {
                    if (string.IsNullOrEmpty(input))
                    {
                        await plan.InvokeNextStepAsync(kernel.CreateNewContext());
                        // or await kernel.StepAsync(plan);
                    }
                    else
                    {
                        plan = await kernel.StepAsync(input, plan);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    break;
                }
                finally
                {
                    var output = PlanToMermaid(plan);
                    if (HybridSupport.IsElectronActive)
                    {
                        Send("got-ask-progress", output);
                    }
                    else
                    {
                        Console.WriteLine(output);
                    }
                }

                if (!plan.HasNextStep)
                {
                    Console.WriteLine($"Step {step} - COMPLETE!");
                    Console.WriteLine(plan.State.ToString());
                    break;
                }

                Console.WriteLine($"Step {step} - Results so far:");
                Console.WriteLine(plan.State.ToString());
            }
        }
        catch (KernelException e)
        {
            Console.WriteLine($"Step - Execution failed:");
            Console.WriteLine(e.Message);
        }

        sw.Stop();
        Console.WriteLine($"Execution complete in {sw.ElapsedMilliseconds} ms!");
        return plan;
    }
}