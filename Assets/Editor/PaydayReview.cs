using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Runs on an isolated, inactive controller; does not reset the player's game.
[InitializeOnLoad]
public static class PaydayReview
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static PaydayReview() { EditorApplication.update += Tick; }

    static void Tick()
    {
        if (!File.Exists("work/run-payday-review") || EditorApplication.isCompiling) return;
        File.Delete("work/run-payday-review");
        Run();
    }

    [MenuItem("Cashflow/Review Payday Accounting")]
    public static void Run()
    {
        Directory.CreateDirectory("work/payday-review");
        var host = new GameObject("Payday accounting test");
        host.SetActive(false);
        try
        {
            var game = host.AddComponent<CashflowGame>();
            Action<string, object> set = (name, value) => typeof(CashflowGame).GetField(name, Flags).SetValue(game, value);
            Func<int> cash = () => (int)typeof(CashflowGame).GetField("cash", Flags).GetValue(game);
            var collect = typeof(CashflowGame).GetMethod("CollectMovementPaydays", Flags);
            set("profession", new CashflowGame.Profession { salary = 2500, other = 1620 });
            set("bankLoan", 1000); // Net Payday must include the $100 loan interest.
            int cases = 0;
            // Independent expected route: Payday spaces are 5, 13 and 21.
            for (int start = 0; start < 24; start++)
            for (int steps = 0; steps <= 12; steps++)
            {
                int crossings = 0;
                foreach (int payday in new[] { 5, 13, 21 })
                {
                    int distance = (payday - start + 24) % 24;
                    if (distance == 0) distance = 24;
                    if (distance <= steps) crossings++;
                }
                set("cash", 400);
                collect.Invoke(game, new object[] { start, steps });
                if (cash() != 400 + crossings * 780)
                    throw new Exception("Incorrect payment from " + start + " moving " + steps);
                cases++;
            }
            set("cash", 400);
            collect.Invoke(game, new object[] { 0, 48 });
            if (cash() != 400 + 6 * 780) throw new Exception("Multiple laps failed");
            set("fastTrack", true);
            set("cash", 400);
            collect.Invoke(game, new object[] { 0, 12 });
            if (cash() != 400 - 2 * 1720) throw new Exception("Fast Track crossed Cashflow Days failed");
            set("fastTrack", false);
            set("cash", 400);
            set("bankLoan", 10000);
            collect.Invoke(game, new object[] { 4, 1 });
            if (cash() != 280) throw new Exception("Negative monthly cashflow accounting changed");
            File.WriteAllText("work/payday-review/results.txt", "PASS: " + cases + " starting-position/roll cases; passing, landing, no double payment, multiple Paydays, wrap without Payday, zero movement, loan interest, negative cashflow, two laps, and Fast Track Cashflow Days.\n");
            Debug.Log("Payday accounting review passed.");
        }
        catch (Exception error)
        {
            File.WriteAllText("work/payday-review/results.txt", "FAIL: " + error);
            Debug.LogException(error);
        }
        finally { UnityEngine.Object.DestroyImmediate(host); }
    }
}
