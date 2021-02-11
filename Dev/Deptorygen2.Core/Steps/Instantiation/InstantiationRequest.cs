﻿using Deptorygen2.Core.Steps.Definitions;
using Deptorygen2.Core.Utilities;

namespace Deptorygen2.Core.Steps.Instantiation
{
	internal record InstantiationRequest(TypeName TypeToResolve,
		ResolverParameterDefinition[] GivenParameters,
		InstantiationMethod Exclude);

	internal record MultipleInstantiationRequest(TypeName[] TypeToResolve,
		ResolverParameterDefinition[] GivenParameters,
		InstantiationMethod Exclude)
	{
		public InstantiationRequest OfType(TypeName type)
		{
			return new InstantiationRequest(type, GivenParameters, Exclude);
		}
	}
}
