{{- for namespace in usingNamespaces -}}
using {{namespace}};
{{~end~}}

namespace {{namespace}};

public class {{className}}
{
	public static IEnumerable<object[]> GetTestCases()
	{
		yield return new object[]
		{
			{{~for member in members ~}}
			{{member.value}}{{if !for.last}},{{end}} //Set value for {{member.name.camelCase}}
			{{~end~}}
		};

		yield return new object[]
		{
			{{~for member in members ~}}
			{{member.value}}{{if !for.last}},{{end}} //Set value for {{member.name.camelCase}}
			{{~end~}}
		};
	}

	public static string GetDisplayName(MethodInfo methodInfo, object[] values)
	{
		return values.ToString();
	}
}