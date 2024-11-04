[TestMethod]
[DynamicData(
	nameof({{className}}.GetTestCases),
	typeof({{className}}),
	DynamicDataSourceType.Method,
	DynamicDataDisplayName = nameof({{className}}.GetDisplayName),
	DynamicDataDisplayNameDeclaringType = typeof({{className}}))]