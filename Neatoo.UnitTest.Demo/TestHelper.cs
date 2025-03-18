using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neatoo.Sandbox;

public static class TestHelper
{
    public static void Verify(string source, string? source2 = null)
    {
        // Parse the provided string into a C# syntax tree
        var syntaxTrees = new List<SyntaxTree>() { CSharpSyntaxTree.ParseText(source), CSharpSyntaxTree.ParseText(SourceAttributes) };
        if (source2 != null)
        {
            syntaxTrees.Add(CSharpSyntaxTree.ParseText(source2));
        }

            // Create a Roslyn compilation for the syntax tree.
            var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: syntaxTrees.ToArray());


        // Create an instance of our EnumGenerator incremental source generator
        var generator = new Neato.BaseGenerator.PartialBaseGenerator();

        // The GeneratorDriver is used to run our generator against a compilation
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        // Run the source generator!
        driver = driver.RunGenerators(compilation);

        driver.GetRunResult().Results.SelectMany(r => r.GeneratedSources).ToList().ForEach(g => Console.WriteLine(g));
        // Use verify to snapshot test the source generator output!
        //return Verifier.Verify(driver);
    }

    public const string SourceAttributes = @"

using System;

namespace Neatoo.RemoteFactory;

[System.AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class FactoryAttribute : Attribute
{
	public FactoryAttribute()
	{
	}
}

[System.AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class SuppressFactoryAttribute : Attribute
{
	public SuppressFactoryAttribute()
	{
	}
}

[System.AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public sealed class RemoteAttribute : Attribute
{
	public RemoteAttribute()
	{
	}
}

[System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class FactoryOperationAttribute : Attribute
{
	public FactoryOperation Operation { get; }

	public FactoryOperationAttribute(FactoryOperation operation)
	{
		this.Operation = operation;
	}
}

[System.AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
public sealed class ServiceAttribute : Attribute
{
	public ServiceAttribute()
	{
	}
}

[System.AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false, AllowMultiple = false)]
public sealed class CreateAttribute : Attribute
{
	public FactoryOperation Operation { get; }

	public CreateAttribute() 
	{
		this.Operation = FactoryOperation.Create;
	}
}

public sealed class FetchAttribute : FactoryOperationAttribute
{
	public FetchAttribute() : base(FactoryOperation.Fetch)
	{
	}
}

public sealed class InsertAttribute : FactoryOperationAttribute
{
	public InsertAttribute() : base(FactoryOperation.Insert)
	{
	}
}

public sealed class UpdateAttribute : FactoryOperationAttribute
{
	public UpdateAttribute() : base(FactoryOperation.Update)
	{
	}
}

public sealed class DeleteAttribute : FactoryOperationAttribute
{
	public DeleteAttribute() : base(FactoryOperation.Delete)
	{
	}
}

public sealed class ExecuteAttribute<TDelegate> : FactoryOperationAttribute
	 where TDelegate : Delegate
{
	public ExecuteAttribute() : base(FactoryOperation.Execute)
	{
	}
}

[System.AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AuthorizeAttribute<T> : Attribute
{
	public AuthorizeAttribute()
	{

	}
}

[System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class AuthorizeAttribute : Attribute
{
	public AuthorizeOperation Operation { get; }
	public AuthorizeAttribute(AuthorizeOperation operation)
	{
		this.Operation = operation;
	}
}

[Flags]
public enum AuthorizeOperation
{
	Create = 1,
	Fetch = 2,
	Insert = 4,
	Update = 8,
	Delete = 16,
	Read = 64,
	Write = 128,
	Execute = 256
}

public enum FactoryOperation
{
	None = 0,
	Execute = AuthorizeOperation.Read,
	Create = AuthorizeOperation.Create | AuthorizeOperation.Read,
	Fetch = AuthorizeOperation.Fetch | AuthorizeOperation.Read,
	Insert = AuthorizeOperation.Insert | AuthorizeOperation.Write,
	Update = AuthorizeOperation.Update | AuthorizeOperation.Write,
	Delete = AuthorizeOperation.Delete | AuthorizeOperation.Write
}



";
}
