﻿using System.Collections.Generic;
using System.Linq;
using Deptorygen2.Core.Interfaces;
using Deptorygen2.Core.Steps.Definitions;
using Deptorygen2.Core.Steps.Semanticses;
using Deptorygen2.Core.Utilities;
using NacHelpers.Extensions;

namespace Deptorygen2.Core.Steps.Instantiation.CreationMethods
{
	internal abstract class CreationMethodBase<T> : IInstantiationCoder
	{
		private readonly Dictionary<TypeName, T> _resolutionSource;

		public abstract InstantiationMethod Method { get; }

		protected CreationMethodBase(FactorySemantics factory, DependencyDefinition[] fields)
		{
			_resolutionSource = GetSource(factory, fields)
				.Select(x => (type: GetTypeInfo(x), source: x))
				.ToDictionary(x => x.type, x => x.source);
		}

		public string? GetCode(InstantiationRequest request, IInstantiationResolver resolver)
		{
			if (request.Exclude == Method
				|| _resolutionSource.GetValueOrDefault(request.TypeToResolve) is not {} resolution)
			{
				return null;
			}

			return GetCreationCode(resolution, request.GivenParameters, resolver);
		}

		protected abstract string GetCreationCode(T resolution,
			ResolverParameterDefinition[] given,
			IInstantiationResolver resolver);

		protected abstract IEnumerable<T> GetSource(FactorySemantics factory, DependencyDefinition[] fields);

		protected abstract TypeName GetTypeInfo(T source);

		protected static string MethodInvocation(IResolverSemantics resolver,
			ResolverParameterDefinition[] given,
			InstantiationMethod exclude,
			IInstantiationResolver injector)
		{
			var request = new MultipleInstantiationRequest(
				resolver.Parameters.Select(x => x.TypeName).ToArray(), given, exclude);
			return $"{resolver.MethodName}({GetArgList(request, injector)})";
		}

		protected static string GetArgList(MultipleInstantiationRequest request,
			IInstantiationResolver resolver)
		{
			return resolver.GetInjections(request).Join(", ");
		}
	}
}
