{{- for namespace in usingNamespaces -}}
using {{namespace}};
{{~end~}}

namespace {{namespace}};

public class {{className}} : IEnumerable<object[]>
{
	public IEnumerator<object[]> GetEnumerator()
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

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}