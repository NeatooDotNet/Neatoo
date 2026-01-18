using System.Text;
using Neatoo.BaseGenerator.Models;

namespace Neatoo.BaseGenerator.Generators;

/// <summary>
/// Generates GetRuleId method for stable rule identification.
/// </summary>
internal static class RuleIdGenerator
{
    /// <summary>
    /// Generates the GetRuleId override method.
    /// </summary>
    public static void GenerateGetRuleIdMethod(StringBuilder sb, RuleExpressionInfo ruleInfo)
    {
        if (ruleInfo.SortedExpressions.IsDefaultOrEmpty)
            return;

        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Generated override for stable rule identification.");
        sb.AppendLine("        /// Maps source expressions to deterministic ordinal IDs.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        protected override uint GetRuleId(string sourceExpression)");
        sb.AppendLine("        {");
        sb.AppendLine("            return sourceExpression switch");
        sb.AppendLine("            {");

        for (int i = 0; i < ruleInfo.SortedExpressions.Count; i++)
        {
            var expr = ruleInfo.SortedExpressions[i];
            var ordinal = (uint)(i + 1);
            var escapedExpr = expr.Replace("\"", "\"\"");
            sb.AppendLine($"                @\"{escapedExpr}\" => {ordinal}u,");
        }

        sb.AppendLine("                _ => base.GetRuleId(sourceExpression) // Fall back to hash for unknown expressions");
        sb.AppendLine("            };");
        sb.AppendLine("        }");
    }
}
