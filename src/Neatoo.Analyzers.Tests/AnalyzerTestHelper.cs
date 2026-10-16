using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace Neatoo.Analyzers.Tests;

/// <summary>
/// Runs an analyzer over source that declares minimal Neatoo stubs, and applies a code fix.
/// The stubs stand in for Neatoo's real types by name and namespace, which is all the analyzers check.
/// Partial properties are written with both parts, and Set{Name} by hand, standing in for generated code.
/// </summary>
public static class AnalyzerTestHelper
{
    public const string NeatooStubs = """
        namespace Neatoo.RemoteFactory
        {
            [System.AttributeUsage(System.AttributeTargets.Class)] public class FactoryAttribute : System.Attribute { }
            [System.AttributeUsage(System.AttributeTargets.Method)] public class CreateAttribute : System.Attribute { }
            [System.AttributeUsage(System.AttributeTargets.Method)] public class FetchAttribute : System.Attribute { }
            [System.AttributeUsage(System.AttributeTargets.Method)] public class InsertAttribute : System.Attribute { }
            [System.AttributeUsage(System.AttributeTargets.Method)] public class UpdateAttribute : System.Attribute { }
            [System.AttributeUsage(System.AttributeTargets.Method)] public class ExecuteAttribute : System.Attribute { }
        }

        namespace Neatoo
        {
            public interface IValidateBase { }
            public interface IEntityBase : IValidateBase { }
            public class EntityLazyLoad<T> { }

            public class ValidateBase<T> where T : ValidateBase<T>
            {
                protected Neatoo.Rules.RuleManager<T> RuleManager => null!;
                public virtual void FactoryComplete(int operation) { }
                public bool IsBusy => false;
                public bool IsValid => true;
            }

            public class EntityBase<T> : ValidateBase<T> where T : EntityBase<T> { public bool IsSavable => false; }
        }

        namespace Neatoo.Rules
        {
            public interface IRuleMessages { }

            public class RuleManager<T>
            {
                public void AddAction(System.Action<T> action, System.Linq.Expressions.Expression<System.Func<T, object?>> trigger) { }
                public void AddActionAsync(System.Func<T, System.Threading.Tasks.Task> action, System.Linq.Expressions.Expression<System.Func<T, object?>> trigger) { }
                public void AddValidation(System.Func<T, string> rule, System.Linq.Expressions.Expression<System.Func<T, object?>> trigger) { }
                public void AddActionAsync(System.Func<T, System.Threading.Tasks.Task> action, params System.Linq.Expressions.Expression<System.Func<T, object?>>[] triggers) { }
                public void AddAction(System.Action<T> action, params System.Linq.Expressions.Expression<System.Func<T, object?>>[] triggers) { }
            }

            // As in Neatoo: the sync RuleBase derives from AsyncRuleBase and runs its Execute synchronously.
            public abstract class RuleBase<T> : AsyncRuleBase<T>
            {
                protected RuleBase(params System.Linq.Expressions.Expression<System.Func<T, object?>>[] triggers) : base(triggers) { }
                protected abstract IRuleMessages Execute(T target);
                protected sealed override System.Threading.Tasks.Task<IRuleMessages> Execute(T target, System.Threading.CancellationToken? token = null)
                    => System.Threading.Tasks.Task.FromResult(Execute(target));
            }

            public abstract class AsyncRuleBase<T>
            {
                protected AsyncRuleBase(params System.Linq.Expressions.Expression<System.Func<T, object?>>[] triggers) { }
                protected void AddTriggerProperties(params System.Linq.Expressions.Expression<System.Func<T, object?>>[] triggers) { }
                protected abstract System.Threading.Tasks.Task<IRuleMessages> Execute(T target, System.Threading.CancellationToken? token = null);
            }
        }
        """;

    public static ImmutableArray<Diagnostic> Analyze(DiagnosticAnalyzer analyzer, string source)
    {
        var compilation = CreateCompilation(source);
        var withAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzer));
        return withAnalyzers.GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult()
            .Where(d => analyzer.SupportedDiagnostics.Any(s => s.Id == d.Id))
            .OrderBy(d => d.Location.SourceSpan.Start)
            .ToImmutableArray();
    }

    /// <summary>
    /// Applies the code fix to the first diagnostic, or returns null when none is offered.
    /// </summary>
    public static string? ApplyFirstFix(DiagnosticAnalyzer analyzer, CodeFixProvider fix, string source)
    {
        using var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);
        var solution = workspace.CurrentSolution
            .AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
            .WithProjectCompilationOptions(projectId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable))
            .WithProjectParseOptions(projectId, new CSharpParseOptions(LanguageVersion.Preview))
            .AddMetadataReferences(projectId, References())
            .AddDocument(documentId, "Test.cs", SourceText.From(source));

        var document = solution.GetDocument(documentId)!;
        var compilation = document.Project.GetCompilationAsync().GetAwaiter().GetResult()!;
        var diagnostic = compilation.WithAnalyzers(ImmutableArray.Create(analyzer))
            .GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult()
            .Where(d => fix.FixableDiagnosticIds.Contains(d.Id))
            .OrderBy(d => d.Location.SourceSpan.Start)
            .FirstOrDefault();
        if (diagnostic == null)
        {
            return null;
        }

        var actions = new List<CodeAction>();
        var context = new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), CancellationToken.None);
        fix.RegisterCodeFixesAsync(context).GetAwaiter().GetResult();
        if (actions.Count == 0)
        {
            return null;
        }

        var operations = actions[0].GetOperationsAsync(CancellationToken.None).GetAwaiter().GetResult();
        var changed = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
        return changed.GetDocument(documentId)!.GetTextAsync().GetAwaiter().GetResult().ToString();
    }

    /// <summary>
    /// Applies the fix's Fix All provider across the document, as the IDE's "Fix all" and
    /// dotnet format do, or returns null when none is offered.
    /// </summary>
    public static string? ApplyFixAll(DiagnosticAnalyzer analyzer, CodeFixProvider fix, string source)
    {
        using var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);
        var solution = workspace.CurrentSolution
            .AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
            .WithProjectCompilationOptions(projectId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable))
            .WithProjectParseOptions(projectId, new CSharpParseOptions(LanguageVersion.Preview))
            .AddMetadataReferences(projectId, References())
            .AddDocument(documentId, "Test.cs", SourceText.From(source));

        var document = solution.GetDocument(documentId)!;
        var compilation = document.Project.GetCompilationAsync().GetAwaiter().GetResult()!;
        var diagnostics = compilation.WithAnalyzers(ImmutableArray.Create(analyzer))
            .GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult()
            .Where(d => fix.FixableDiagnosticIds.Contains(d.Id))
            .ToImmutableArray();
        var provider = fix.GetFixAllProvider();
        if (diagnostics.IsEmpty || provider == null)
        {
            return null;
        }

        var equivalenceKey = FirstEquivalenceKey(fix, document, diagnostics[0]);
        if (equivalenceKey == null)
        {
            return null;
        }

        var context = new FixAllContext(
            document, fix, FixAllScope.Document, equivalenceKey, fix.FixableDiagnosticIds,
            new FixedDiagnosticProvider(diagnostics), CancellationToken.None);
        var action = provider.GetFixAsync(context).GetAwaiter().GetResult();
        if (action == null)
        {
            return null;
        }

        var operations = action.GetOperationsAsync(CancellationToken.None).GetAwaiter().GetResult();
        var changed = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
        return changed.GetDocument(documentId)!.GetTextAsync().GetAwaiter().GetResult().ToString();
    }

    private static string? FirstEquivalenceKey(CodeFixProvider fix, Document document, Diagnostic diagnostic)
    {
        var actions = new List<CodeAction>();
        var context = new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), CancellationToken.None);
        fix.RegisterCodeFixesAsync(context).GetAwaiter().GetResult();
        return actions.FirstOrDefault()?.EquivalenceKey;
    }

    private sealed class FixedDiagnosticProvider(ImmutableArray<Diagnostic> diagnostics) : FixAllContext.DiagnosticProvider
    {
        public override Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken)
            => Task.FromResult<IEnumerable<Diagnostic>>(diagnostics);

        public override Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken)
            => Task.FromResult<IEnumerable<Diagnostic>>(diagnostics);

        public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken)
            => Task.FromResult<IEnumerable<Diagnostic>>([]);
    }

    /// <summary>
    /// Compile errors in the source, so a test can show a fixed document still compiles.
    /// </summary>
    public static ImmutableArray<Diagnostic> CompileErrors(string source)
        => CreateCompilation(source).GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToImmutableArray();

    private static CSharpCompilation CreateCompilation(string source)
        => CSharpCompilation.Create(
            "TestAssembly",
            [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview))],
            References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

    private static List<MetadataReference> References()
    {
        var runtimePath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var names = new[] { "System.Runtime.dll", "System.Collections.dll", "System.Linq.dll", "System.Linq.Expressions.dll", "System.Threading.Tasks.dll", "netstandard.dll" };
        var references = names
            .Select(n => Path.Combine(runtimePath, n))
            .Where(File.Exists)
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
            .ToList();
        references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        return references;
    }
}
