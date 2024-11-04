public static IEnumerable<object[]> Get{{testMethodName}}Cases()
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