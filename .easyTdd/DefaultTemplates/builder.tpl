{{- for namespace in usingNamespaces -}}
using {{namespace}};
{{~end~}}

namespace {{namespace}}
{
	public partial class {{className}}
	{
		public static {{className}} Default()
		{
			return new {{className}}(
			{{~for member in members ~}}
				() => default{{if !for.last}},{{else}} {{end}} // Set default {{member.name.camelCase}} value
			{{~end~}}
			);
		}
	}
}