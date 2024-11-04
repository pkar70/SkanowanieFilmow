private static IEnumerable<TestCaseData> Get{{testMethodName}}Cases()
{
	yield return new TestCaseData(
		{{~for member in members ~}}
			{{member.value}}{{if !for.last}},{{end}} //Set value for {{member.name.camelCase}}
		{{~end~}}
		)
		.SetName("[Test display name goes here]");

	yield return new TestCaseData(
		{{~for member in members ~}}
			{{member.value}}{{if !for.last}},{{end}} //Set value for {{member.name.camelCase}}
		{{~end~}}
		)
		.SetName("[Test display name goes here]");
}