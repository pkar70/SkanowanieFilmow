{{-func hasResult(member)
	ret member.hasResult
end}}

{{-func hasGetter(member)
	ret member.hasGetter
end}}

{{-func hasSetter(member)
	ret member.hasSetter
end}}

{{-func hasOnlySetter(member)
	ret !member.hasGetter && member.hasSetter
end}}

{{-func doesNotHaveResult(member)
	ret !member.hasResult
end}}

{{- for namespace in usingNamespaces -}}
using {{namespace}};
{{~end~}}

namespace {{namespace}}
{
	public class {{className}}
	{
		{{~for method in methods~}}
			{{~for parameter in method.parameters~}}
		private {{parameter.type}} _{{parameter.fieldName.camelCase}};
			{{~end~}}
		{{~end~}}

		public {{className}}()
		{
			{{-for method in methods}}
				{{-for parameter in method.parameters}}
			_{{parameter.fieldName.camelCase}} = {{parameter.value}};{{this.hasMethodsParameters = true}}
				{{-end}}
			{{-end}}
		}
		{{-for method in methods}}
		{{-for parameter in method.parameters}}

		[Theory]
		[InlineData(null)]
		public {{if method.isTask}}async Task{{else}}void{{end}} {{method.name}}ThrowsArgumentExceptionWhen{{parameter.originalName.pascalCase}}IsNotValid(
			{{parameter.type}} {{parameter.fieldName.camelCase}})
		{
			_{{parameter.fieldName.camelCase}} = {{parameter.fieldName.camelCase}};

			{{if method.isTask}}Func<Task>{{else}}Action{{end}} act = {{if method.isTask}}async () => await{{else}}() =>{{end}} Call{{method.name}}();

			act
				.Should()
				.Throw<ArgumentException>();
		}
		{{-end}}
		{{-end-}}

		{{~for method in methods}}

		private {{if method.isTask}}async {{end}}{{method.returnType}} Call{{method.name}}()
		{
			var sut = Create();

			{{~if method.hasResult}}			return {{else}}			{{end}}{{if method.isTask}}await {{end}}sut
				.{{method.fullName}}({{if (method.parameters | array.size) == 0 }});{{else}}
					{{-for parameter in method.parameters}}
					_{{parameter.fieldName.camelCase}}{{if !for.last}},{{end}}
					{{-end}}
				);{{end}}
		}
		{{-end}}

		private {{targetTypeName}} Create()
		{
			return new {{targetTypeName}}();
		}
	}
}