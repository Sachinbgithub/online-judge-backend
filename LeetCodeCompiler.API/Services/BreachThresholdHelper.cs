using LeetCodeCompiler.API.Models;

namespace LeetCodeCompiler.API.Services
{
    public static class BreachThresholdHelper
    {
        /// <summary>
        /// Derives warning/flag thresholds from breachRuleLimit.
        /// When breachRuleLimit is 0, all three rules are disabled.
        /// </summary>
        public static void ApplyBreachDefaults(CodingTest test)
        {
            if (test.BreachRuleLimit <= 0)
            {
                test.WarningThreshold = 0;
                test.FlagThreshold = 0;
                return;
            }

            test.WarningThreshold = Math.Max(1, test.BreachRuleLimit - 2);
            test.FlagThreshold = Math.Max(1, test.BreachRuleLimit - 1);
        }
    }
}
