using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ElectronNET.API;
using ElectronNET.WebApp.Controllers;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;

namespace ElectronNET.WebApp.Utils;

internal static class RepoFiles
{
    /// <summary>
    /// Scan the local folders from the repo, looking for "samples/skills" folder.
    /// </summary>
    /// <returns>The full path to samples/skills</returns>
    internal static string SkillsPath()
    {
        bool SearchPath(string pathToFind, out string result, int maxAttempts = 10)
        {
            //Console.WriteLine("Assembly.GetExecutingAssembly().Location: " + Assembly.GetExecutingAssembly().CodeBase);
            //var currDir = Path.GetFullPath(Assembly.GetExecutingAssembly().Location);
            var currDir = Path.GetFullPath("C:\\src\\MindGPT");
            Console.WriteLine(currDir);
            bool found;
            do
            {
                result = Path.Join(currDir, pathToFind);
                Console.WriteLine(result);
                found = Directory.Exists(result);
                currDir = Path.GetFullPath(Path.Combine(currDir, ".."));
            } while (maxAttempts-- > 0 && !found);

            Console.WriteLine(found);
            return found;
        }

        if (!SearchPath("Skills", out string path))
        {
            throw new Exception("Skills directory not found. The app needs the skills from the repo to work.");
        }

        return path;
    }

    public static string PlanToMermaid(Plan plan)
    {
        var nameList = new List<string>();
        var relationshipList = new List<string>();
        var queue = new Queue<(int, Plan)>();
        queue.Enqueue((0, plan));

        while (queue.Count > 0)
        {
            var (sequence, step) = queue.Dequeue();
            var desc = !string.IsNullOrEmpty(step.Description) && step.Description != step.Name ? $": {step.Description}" : string.Empty;
            var name = sequence == 0 ? $"seq{sequence}(\"GOAL" : $"seq{sequence}(\"{sequence}. {step.Name}{desc}";
            name += "<br>" + string.Join("<br>", step.NamedParameters.Select((k, v) => $"{k}: {v}")) + "\")";;
            nameList.Add(name);

            for (int i = 0; i < step.Steps.Count; i++)
            {
                queue.Enqueue((sequence + i + 1, step.Steps[i]));
                relationshipList.Add($"seq{sequence} --> seq{sequence + i + 1}");
                if (step.NextStepIndex > 0)
                {
                    relationshipList.Add($"class seq{sequence} progress");
                }

                if (step.NextStepIndex > 0 && step.NextStepIndex < i)
                {
                    relationshipList.Add($"class seq{sequence + i + 1} pending");
                }
                else if(step.NextStepIndex == i)
                {
                    relationshipList.Add($"class seq{sequence + i + 1} progress");
                }
                else if(step.NextStepIndex > i)
                {
                    relationshipList.Add($"class seq{sequence + i + 1} completed");
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
        return string.Join("\n", nameList) + "\n" + string.Join("\n", relationshipList);
    }

    public static async Task<Plan> ExecutePlanAsync(
        IKernel kernel,
        Plan plan,
        Action<Plan> progress,
        Action<Plan> spin = null,
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
                    if(plan.HasNextStep) spin?.Invoke(plan.Steps[plan.NextStepIndex]);
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
                    progress?.Invoke(plan);
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
        spin?.Invoke(null);
        return plan;
    }
}