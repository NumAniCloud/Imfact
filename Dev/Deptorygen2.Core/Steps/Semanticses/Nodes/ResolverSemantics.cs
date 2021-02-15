﻿using System.Collections.Generic;
using System.Linq;
using Deptorygen2.Annotations;
using Deptorygen2.Core.Interfaces;
using Deptorygen2.Core.Steps.Aggregation;
using Deptorygen2.Core.Steps.Semanticses.Nodes;
using Deptorygen2.Core.Utilities;
using Microsoft.CodeAnalysis;
using IServiceProvider = Deptorygen2.Core.Interfaces.IServiceProvider;

namespace Deptorygen2.Core.Steps.Semanticses
{
	internal record ResolverSemantics(string MethodName,
		TypeName ReturnType,
		ResolutionSemantics? ReturnTypeResolution,
		ResolutionSemantics[] Resolutions,
		ParameterSemantics[] Parameters,
		Accessibility Accessibility,
		HookSemantics[] Hooks) : IServiceConsumer, IServiceProvider, INamespaceClaimer, IResolverSemantics
	{
		public IEnumerable<TypeName> GetRequiredServiceTypes()
		{
			return ReturnTypeResolution.AsEnumerable()
				.Concat(Resolutions)
				.SelectMany(x => x.Dependencies)
				.Except(Parameters.Select(x => x.TypeName));
		}

		public IEnumerable<TypeName> GetCapableServiceTypes()
		{
			yield return ReturnType;
		}

		public IEnumerable<string> GetRequiredNamespaces()
		{
			yield return ReturnType.FullNamespace;
			foreach (var parameter in Parameters)
			{
				yield return parameter.TypeName.FullNamespace;
			}

			if (Resolutions.Any())
			{
				yield return Resolutions[0].TypeName.FullNamespace;
			}
		}

		public static Builder<MethodToAnalyze,
			(ResolutionSemantics?,
			ResolutionSemantics[],
			ParameterSemantics[],
			HookSemantics[]),
			ResolverSemantics>? GetBuilder(MethodToAnalyze method)
		{
			if (!method.IsSingleResolver())
			{
				return null;
			}
			
			var ctxType = new ParameterSemantics(TypeName.FromType(typeof(ResolutionContext)), "context");

			return new(method, tuple => new ResolverSemantics(
				"__" + method.Symbol.Name,
				TypeName.FromSymbol(method.Symbol.ReturnType),
				tuple.Item1,
				tuple.Item2,
				tuple.Item3.Append(ctxType).ToArray(),
				method.Symbol.DeclaredAccessibility,
				tuple.Item4));
		}
	}
}
