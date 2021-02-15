﻿using Deptorygen2.Core.Steps.Semanticses;
using Deptorygen2.Core.Steps.Semanticses.Nodes;
using Deptorygen2.Core.Utilities;
using Microsoft.CodeAnalysis;

namespace Deptorygen2.Core.Interfaces
{
	internal interface IResolverSemantics
	{
		TypeName ReturnType { get; }
		string MethodName { get; }
		ParameterSemantics[] Parameters { get; }
		Accessibility Accessibility { get; }
		ResolutionSemantics[] Resolutions { get; }
		HookSemantics[] Hooks { get; }
	}
}