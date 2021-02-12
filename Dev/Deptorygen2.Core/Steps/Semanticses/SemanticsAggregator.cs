﻿using System;
using System.Collections.Generic;
using System.Linq;
using Deptorygen2.Core.Interfaces;
using Deptorygen2.Core.Steps.Aggregation;
using NacHelpers.Extensions;

namespace Deptorygen2.Core.Steps.Semanticses
{
	internal class SemanticsAggregator
	{
		private readonly IAnalysisContext _context;

		public SemanticsAggregator(IAnalysisContext context)
		{
			_context = context;
		}

		public GenerationSemantics? Aggregate(ClassToAnalyze @class, IAnalysisContext context)
		{
			var methods = @class.GetMethods(context);
			var properties = @class.GetProperties(context);

			return GenerationSemantics.GetBuilder(@class).Build(_ =>
			{
				var factory = FactorySemantics.GetBuilder(@class)?.Build(_ =>
				{
					var resolvers = AggregateResolvers(methods);
					var collectionResolvers = AggregateCollectionResolvers(methods);

					var delegations = properties.Select(DelegationSemantics.GetBuilder).Build(p =>
					{
						var dr = AggregateResolvers(methods);
						var dcr = AggregateCollectionResolvers(methods);
						return (dr, dcr);
					});

					return (resolvers, collectionResolvers, delegations);
				});

				if (factory is null)
				{
					return (new string[0], null, new DependencySemantics[0]);
				}

				var dependencies = DependencySemantics.FromFactory(factory);
				var namespaces = AggregateNamespaces(factory).ToArray();

				return (namespaces, factory, dependencies);
			});
		}

		private ResolverSemantics[] AggregateResolvers(MethodToAnalyze[] methods)
		{
			return methods.Select(ResolverSemantics.GetBuilder).Build(m =>
			{
				var ret = m.GetReturnType(_context) is { } t
					? ResolutionSemantics.Build(t)
					: null;
				var (parameters, resolutions) = LoadMethodFeature(m.GetParameters(), m.GetAttributes());

				return (ret, resolutions, parameters);
			});
		}

		private CollectionResolverSemantics[] AggregateCollectionResolvers(MethodToAnalyze[] methods)
		{
			return methods.Select(CollectionResolverSemantics.GetBuilder).Build(
				m => LoadMethodFeature(m.GetParameters(), m.GetAttributes()));
		}

		private (ParameterSemantics[], ResolutionSemantics[]) LoadMethodFeature(
			ParameterToAnalyze[] parameters, AttributeToAnalyze[] attributes)
		{
			var ps = Build(parameters, p => ParameterSemantics.Build(p, _context));
			var rs = Build(attributes, a => ResolutionSemantics.Build(a, _context));
			return (ps, rs);
		}

		private TResult[] Build<T, TResult>(IEnumerable<T> source, Func<T, TResult?> selector)
			where TResult : class
		{
			return source.Select(selector)
				.FilterNull()
				.ToArray();
		}

		private static IEnumerable<string> AggregateNamespaces(FactorySemantics semantics)
		{
			return semantics.Resolvers.Cast<INamespaceClaimer>()
				.Concat(semantics.CollectionResolvers)
				.Concat(semantics.Delegations)
				.SelectMany(x => x.GetRequiredNamespaces())
				.Distinct();
		}
	}
}
