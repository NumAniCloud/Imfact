﻿using Deptorygen2.Core.Entities;
using Deptorygen2.Core.Steps.Semanticses.Nodes;
using Deptorygen2.Core.Utilities;

namespace Deptorygen2.Core.Steps.Semanticses.Interfaces
{
	internal interface IFactorySemantics
	{
		TypeNode Type { get; }
		Resolver[] Resolvers { get; }
		MultiResolver[] MultiResolvers { get; }
	}
}
