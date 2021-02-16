﻿using System.Collections.Generic;
using System.Linq;
using Deptorygen2.Annotations;
using Deptorygen2.Core.Interfaces;
using Deptorygen2.Core.Steps.Aspects.Nodes;
using Deptorygen2.Core.Utilities;
using Microsoft.CodeAnalysis;
using IServiceProvider = Deptorygen2.Core.Interfaces.IServiceProvider;

namespace Deptorygen2.Core.Steps.Semanticses.Nodes
{
	internal record MultiResolver(ResolverCommon Common)
		: IServiceConsumer, IServiceProvider, INamespaceClaimer, IResolverSemantics
	{
		public TypeName ReturnType => Common.ReturnType;
		public string MethodName => Common.MethodName;
		public Parameter[] Parameters => Common.Parameters;
		public Accessibility Accessibility => Common.Accessibility;
		public Resolution[] Resolutions => Common.Resolutions;
		public Hook[] Hooks => Common.Hooks;

		public TypeName ElementType => ReturnType.TypeArguments[0];

		public IEnumerable<TypeName> GetRequiredServiceTypes()
		{
			return Resolutions.SelectMany(x => x.Dependencies)
				.Except(Parameters.Select(x => x.TypeName));
		}

		public IEnumerable<TypeName> GetCapableServiceTypes()
		{
			yield return ReturnType;
		}

		public IEnumerable<string> GetRequiredNamespaces()
		{
			yield return "System.Collections.Generic";
			yield return ReturnType.TypeArguments[0].FullNamespace;

			foreach (var parameter in Parameters)
			{
				yield return parameter.TypeName.FullNamespace;
			}

			foreach (var resolution in Resolutions)
			{
				yield return resolution.TypeName.FullNamespace;
			}
		}

		public static Builder<Method,
			(Parameter[], Resolution[], Hook[]),
			MultiResolver>? GetBuilder(Method method)
		{
			if (!method.IsCollectionResolver())
			{
				return null;
			}

			var ctxType = new Parameter(TypeName.FromType(typeof(ResolutionContext)), "context");

			return null;
		}
	}
}
