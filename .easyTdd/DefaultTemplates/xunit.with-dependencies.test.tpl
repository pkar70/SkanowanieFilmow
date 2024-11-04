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

{{-func isAbstract(dependency)
	ret dependency.isAbstract
end}}

{{-func isConcrete(dependency)
	ret !dependency.isAbstract
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
			{{~if method.parameters | array.size > 0~}}

			{{~end-}}
		{{~end-}}

		{{-for dependency in dependencies~}}
			{{~for method in dependency.methods | array.filter @hasResult~}}
		private {{method.resultFieldType}} _{{method.resultFieldName.camelCase}};
			{{~end~}}
			{{~for property in dependency.properties | array.filter @hasGetter~}}
		private {{property.type}} _{{property.resultFieldName.camelCase}};
			{{~end~}}
		{{~end-}}
		
		{{-if dependencies | array.size > 0~}}

		{{~end-}}

		{{-for dependency in dependencies~}}
		private {{dependency.testDoubleType}} _{{dependency.testDoubleFieldName.camelCase}};
		{{~end-}}
		
		{{-if dependencies | array.size > 0~}}

		{{~end~}}
		public {{className}}()
		{
			{{-for method in methods}}
				{{-for parameter in method.parameters}}
			_{{parameter.fieldName.camelCase}} = {{parameter.value}};{{this.hasMethodsParameters = true}}
				{{-end}}
			{{-end~}}
		
			{{-if dependencies | array.size > 0 && this.hasMethodsParameters~}}

			{{~end-}}

			{{-for dependency in dependencies}}
				{{-for method in dependency.methods | array.filter @hasResult}}
			_{{method.resultFieldName.camelCase}} = default;
				{{-end}}
				{{-for property in dependency.properties | array.filter @hasGetter}}
			_{{property.resultFieldName.camelCase}} = default;
				{{-end}}
			{{-end~}}

			{{-for dependency in dependencies}}
				{{-if dependency.isAbstract}}

			_{{dependency.testDoubleFieldName.camelCase}} = {{dependency.testDoubleType}}
				.Create()
				{{-for method in dependency.methods | array.filter @hasResult}}
				.Setup{{method.fullName}}(() => _{{method.resultFieldName.camelCase}})
				{{-end}}
				{{-for method in dependency.methods | array.filter @doesNotHaveResult}}
				.Setup{{method.fullName}}()
				{{-end}}
				{{-for property in dependency.properties | array.filter @hasGetter}}
				.Setup{{property.name.pascalCase}}(() => _{{property.resultFieldName.camelCase}})
				{{-end}}
				{{-for property in dependency.properties | array.filter @hasOnlySetter}}
				.Setup{{property.name.pascalCase}}()
				{{-end}};
				{{-else}}

			_{{dependency.testDoubleFieldName.camelCase}} = {{dependency.testDoubleType}}
				.Default()
				{{-for property in dependency.properties | array.filter @hasGetter}}
				.With{{property.name.pascalCase}}(() => _{{property.resultFieldName.camelCase}})
				{{-end}};
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

		{{-for dependency in dependencies | array.filter @isAbstract}}
			{{- for dependencyMethod in dependency.methods}}
				{{-for method in methods}}

		[Fact]
		public {{if method.isTask}}async Task{{else}}void{{end}} {{method.name}}Calls{{dependency.originalName.pascalCase}}{{dependencyMethod.name}}Once()
		{
			{{if method.isTask}}await {{end}}Call{{method.name}}();

			_{{dependency.testDoubleFieldName.camelCase}}
				.Verify(
					x => x.{{dependencyMethod.name}}({{if (dependencyMethod.parameters | array.size) == 0 }}),{{else}}
					{{-for parameter in dependencyMethod.parameters}}
						{{if parameter.relevantFieldName}}_{{parameter.relevantFieldName.camelCase}}{{else}}It.IsAny<{{parameter.type}}>(){{end}}{{if !for.last}},{{end}}
					{{-end}}
					),{{end}}
					Times.Once
				);

			_{{dependency.testDoubleFieldName.camelCase}}
				.VerifyNoOtherCalls();
		}
				{{-end-}}
			{{-end-}}
			{{- for dependencyProperty in dependency.properties | array.filter @hasGetter}}
				{{-for method in methods}}

		[Fact]
		public {{if method.isTask}}async Task{{else}}void{{end}} {{method.name}}Calls{{dependency.originalName.pascalCase}}{{dependencyProperty.name.pascalCase}}GetOnce()
		{
			{{if method.isTask}}await {{end}}Call{{method.name}}();

			_{{dependency.testDoubleFieldName.camelCase}}
				.VerifyGet(
					x => x.{{dependencyProperty.name.pascalCase}},
					Times.Once
				);

			_{{dependency.testDoubleFieldName.camelCase}}
				.VerifyNoOtherCalls();
		}
				{{-end-}}
			{{-end-}}
			{{- for dependencyProperty in dependency.properties | array.filter @hasSetter}}
				{{-for method in methods}}

		[Fact]
		public {{if method.isTask}}async Task{{else}}void{{end}} {{method.name}}Calls{{dependency.originalName.pascalCase}}{{dependencyProperty.name.pascalCase}}SetOnce()
		{
			{{if method.isTask}}await {{end}}Call{{method.name}}();

			_{{dependency.testDoubleFieldName.camelCase}}
				.VerifySet(
					x => x.{{dependencyProperty.name.pascalCase}} = It.IsAny<{{dependencyProperty.type}}>(),
					Times.Once
				);

			_{{dependency.testDoubleFieldName.camelCase}}
				.VerifyNoOtherCalls();
		}
				{{-end-}}
			{{-end-}}
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
			return new {{targetTypeName}}({{if (dependencies | array.size) == 0 }});{{else}}
			{{-for dependency in dependencies}}
				_{{dependency.testDoubleFieldName.camelCase}}.{{if dependency.isAbstract}}Object{{else}}Build(){{end}}{{if !for.last}},{{end}}
			{{-end}}
			);{{end}}
		}
	}
}