{{- for namespace in usingNamespaces -}}
using {{namespace}};
{{~end~}}

namespace {{namespace}}
{
	[EasyTdd.Generators.FluentMockFor(typeof({{targetTypeOpenName}}))]
	public partial class {{className}}
	{
		public static {{className}} Create()
		{
			return new {{className}}();
		}
	}
}