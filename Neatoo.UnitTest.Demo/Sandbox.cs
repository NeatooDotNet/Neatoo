
using Xunit;

namespace Neatoo.Sandbox;

public class Sandbox
{


	[Fact]
   public void Test1()
   {
		// The source code to test
		var source = @"
using Neatoo;
using Neatoo.RemoteFactory;

namespace Neatoo {


public class Base<T> {

}

}
";

		var source2 = @"

using Neatoo;

namespace Neatoo.Sandbox {

	public partial interface IBaseObject {

	}


	public partial class BaseObject : Base<BaseObject>, IBaseObject {

		public partial int PropertyA { get; set; }
		public partial int PropertyB { get; set; }

	}

}
";

		// Pass the source code to our helper and snapshot test the output
		TestHelper.Verify(source, source2);
	}
}