﻿using System.Collections.Generic;
using System.Linq;
using Deptorygen2.Core.Interfaces;
using Deptorygen2.Core.Steps.Semanticses;
using Deptorygen2.Core.Steps.Semanticses.Nodes;
using NacHelpers.Extensions;

namespace Deptorygen2.Core.Steps.Expressions
{
	internal class ExpressionBuilder
	{
		private readonly SemanticsRoot _semantics;
		private readonly CreationCrawler _crawler;
		private readonly UsingRule _usingRule = new();

		public ExpressionBuilder(SemanticsRoot semantics)
		{
			_semantics = semantics;
			_crawler = new CreationCrawler(semantics);
		}

		public ResolutionRoot Build()
		{
			var injectionResult = BuildInjection();
			var usings = _usingRule.Extract(_semantics, injectionResult);
			var disposable = DisposableInfo.Aggregate(_semantics.Factory,
				injectionResult.Dependencies);

			return new ResolutionRoot(_semantics, injectionResult, usings, disposable);
		}

		private InjectionResult BuildInjection()
		{
			var result = BuildResolverExp();
			var resultMulti = BuildMultiResolverExp();

			var deps = resultMulti.Values.SelectMany(x => x.Roots)
				.Concat(result.Values.Select(x => x.Root))
				.SelectMany(FindFields)
				.GroupBy(x => x.Type.Record)
				.Select(x => x.First())
				.Select(x => new Dependency(x.Type, x.Name))
				.ToArray();

			var injectionResult = new InjectionResult(result, resultMulti, deps);
			return injectionResult;
		}

		private Dictionary<IResolverSemantics, MultiCreationExpTree> BuildMultiResolverExp()
		{
			Dictionary<IResolverSemantics, MultiCreationExpTree> resultMulti = new();
			var multi = _semantics.Factory.MultiResolvers;
			foreach (var multiResolver in multi)
			{
				var context = new CreationContext(multiResolver,
					multiResolver.Resolutions.Select(x => x.TypeName).ToArray(),
					new List<Parameter>(),
					_crawler);
				var creations = _crawler.GetExpression(context);
				resultMulti[multiResolver] = new MultiCreationExpTree(creations.ToArray());
			}

			return resultMulti;
		}

		private Dictionary<IResolverSemantics, CreationExpTree> BuildResolverExp()
		{
			Dictionary<IResolverSemantics, CreationExpTree> result = new();
			var methods = _semantics.Factory.Resolvers;
			foreach (var method in methods)
			{
				var context = new CreationContext(method,
					method.ActualResolution.TypeName.WrapByArray(),
					new List<Parameter>(),
					_crawler);
				var creation = _crawler.GetExpression(context).First();
				result[method] = new CreationExpTree(creation);
			}

			return result;
		}

		private IEnumerable<UnsatisfiedField> FindFields(ICreationNode pivot)
		{
			if (pivot is UnsatisfiedField field)
			{
				yield return field;
			}
			else if (pivot is Invocation invocation)
			{
				foreach (var arg in invocation.Arguments)
				{
					foreach (var child in FindFields(arg))
					{
						yield return child;
					}
				}
			}
		}
	}
}
