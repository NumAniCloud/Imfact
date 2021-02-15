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
		Parameter[] Parameters { get; }
		Accessibility Accessibility { get; }
		Resolution[] Resolutions { get; }
		Hook[] Hooks { get; }
	}
}
