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
        sb.AppendLine("        /// Maps source expressions to deterministic FNV-1a hash IDs.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        protected override uint GetRuleId(string sourceExpression)");
        sb.AppendLine("        {");
        sb.AppendLine("            return sourceExpression switch");
        sb.AppendLine("            {");

        for (int i = 0; i < ruleInfo.SortedExpressions.Count; i++)
        {
            var expr = ruleInfo.SortedExpressions[i];
            var hash = ComputeFnv1aHash(expr);
            var escapedExpr = expr.Replace("\"", "\"\"");
            sb.AppendLine($"                @\"{escapedExpr}\" => 0x{hash:X8}u,");
        }

        sb.AppendLine("                _ => base.GetRuleId(sourceExpression) // Fall back to base for unknown expressions");
        sb.AppendLine("            };");
        sb.AppendLine("        }");
    }

    /// <summary>
    /// Computes FNV-1a hash for a source expression.
    /// Must match ValidateBase.ComputeRuleIdHash exactly.
    /// </summary>
    private static uint ComputeFnv1aHash(string sourceExpression)
    {
        unchecked
        {
            uint hash = 2166136261; // FNV-1a offset basis
            foreach (char c in sourceExpression)
            {
                hash ^= c;
                hash *= 16777619; // FNV-1a prime
            }
            return hash;
        }
    }
}
