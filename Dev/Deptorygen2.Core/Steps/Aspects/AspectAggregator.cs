﻿using System.Collections.Generic;
using Deptorygen2.Annotations;
using Deptorygen2.Core.Interfaces;
using Deptorygen2.Core.Steps.Aspects.Nodes;
using Deptorygen2.Core.Utilities;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using Deptorygen2.Core.Entities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NacHelpers.Extensions;

namespace Deptorygen2.Core.Steps.Aspects
{
	internal class AspectAggregator
	{
		readonly AttributeName _resAt = new(nameof(ResolutionAttribute));
		readonly AttributeName _hokAt = new(nameof(HookAttribute));
		readonly AttributeName _cacAt = new(nameof(CacheAttribute));
		readonly AttributeName _cprAt = new(nameof(CachePerResolutionAttribute));

		public Class? Aggregate(ClassDeclarationSyntax root, IAnalysisContext context)
		{
			return context.GetNamedTypeSymbol(root) is { } symbol
				? new Class(root, symbol)
				: null;
		}

		public ClassAspect? Aggregatexx(ClassDeclarationSyntax root, IAnalysisContext context)
		{
			var hasAttr = root.AttributeLists.HasAttribute(new AttributeName(nameof(FactoryAttribute)));
			var isPartial = root.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));

			if (!hasAttr || !isPartial)
			{
				return null;
			}

			if (context.GetNamedTypeSymbol(root) is not { } symbol)
			{
				return null;
			}

			var baseClasses = TraverseBase(symbol).Select(x =>
			{
				var syntax = x.DeclaringSyntaxReferences
					.Select(y => y.GetSyntax())
					.OfType<ClassDeclarationSyntax>()
					.FirstOrDefault();
				return syntax is not null
					? ExtractAspect(syntax, context)
					: null;
			}).FilterNull().ToArray();

			return ExtractAspect(root, context, baseClasses);

			static IEnumerable<INamedTypeSymbol> TraverseBase(INamedTypeSymbol pivot)
			{
				if (pivot.BaseType is not null)
				{
					yield return pivot.BaseType;
					foreach (var baseSymbol in TraverseBase(pivot.BaseType))
					{
						yield return baseSymbol;
					}
				}
			}
		}

		private ClassAspect ExtractAspect(ClassDeclarationSyntax syntax,
			IAnalysisContext context, ClassAspect[]? baseClasses = null)
		{
			var methods = syntax.Members
				.OfType<MethodDeclarationSyntax>()
				.Select(m => ExtractAspect(m, context))
				.FilterNull()
				.ToArray();

			var properties = syntax.Members
				.OfType<PropertyDeclarationSyntax>()
				.Select(m => ExtractAspect(m, context))
				.FilterNull()
				.ToArray();

			return new ClassAspect(baseClasses ?? new ClassAspect[0],
				methods, properties);
		}

		private MethodAspect? ExtractAspect(MethodDeclarationSyntax syntax,
			IAnalysisContext context)
		{
			if (context.GetMethodSymbol(syntax) is not { } symbol)
			{
				return null;
			}

			if (symbol.ReturnType is not INamedTypeSymbol returnSymbol)
			{
				return null;
			}

			var returnType = ExtractReturnTypeAspect(returnSymbol);
			var attributes = symbol.GetAttributes()
				.Select(x => ExtractAspect(x, returnSymbol, symbol.Name))
				.FilterNull()
				.ToArray();
			var parameters = syntax.ParameterList.Parameters
				.Select(x => ExtractAspect(x, context))
				.FilterNull()
				.ToArray();

			return new MethodAspect(GetKind(), returnType, attributes, parameters);

			ResolverKind GetKind()
			{
				var idSymbol = returnSymbol.ConstructedFrom;
				var typeNameValid = idSymbol.MetadataName == typeof(IEnumerable<>).Name;
				var typeArgValid = idSymbol.TypeArguments.Length == 1;
				return typeNameValid && typeArgValid ? ResolverKind.Multi : ResolverKind.Single;
			}
		}

		private ReturnTypeAspect ExtractReturnTypeAspect(INamedTypeSymbol symbol)
		{
			return new(TypeNode.FromSymbol(symbol), symbol.IsAbstract);
		}

		private ParameterAspect? ExtractAspect(ParameterSyntax syntax, IAnalysisContext context)
		{
			if (syntax.Type is null || context.GetTypeSymbol(syntax.Type) is not { } symbol)
			{
				return null;
			}

			return new ParameterAspect(TypeNode.FromSymbol(symbol), symbol.Name);
		}

		private MethodAttributeAspect? ExtractAspect(AttributeData data,
			INamedTypeSymbol ownerReturn,
			string ownerName)
		{
			if (data.AttributeClass?.Name is not { } name)
			{
				return null;
			}

			AnnotationKind kind;

			if (_resAt.MatchWithAnyName(name))
			{
				if (data.ConstructorArguments.Length == 1
					&& data.ConstructorArguments[0].Kind == TypedConstantKind.Type)
				{
					kind = AnnotationKind.Resolution;
				}
				else
				{
					return null;
				}
			}
			else if (_hokAt.MatchWithAnyName(name))
			{
				if (data.ConstructorArguments.Length == 1
					&& data.ConstructorArguments[0].Kind == TypedConstantKind.Type
					&& data.ConstructorArguments[0].Value is INamedTypeSymbol arg
					&& arg.ConstructedFrom.IsImplementing(typeof(IHook<>)))
				{
					kind = AnnotationKind.Hook;
				}
				else
				{
					return null;
				}
			}
			else if (_cacAt.MatchWithAnyName(name))
			{
				kind = AnnotationKind.CacheHookPreset;
			}
			else if (_cprAt.MatchWithAnyName(name))
			{
				kind = AnnotationKind.CachePrHookPreset;
			}
			else
			{
				return null;
			}

			return new MethodAttributeAspect(kind, TypeNode.FromSymbol(ownerReturn), ownerName);
		}

		private PropertyAspect? ExtractAspect(PropertyDeclarationSyntax syntax,
			IAnalysisContext context)
		{
			if (context.GetPropertySymbol(syntax) is not { } symbol)
			{
				return null;
			}

			var isDelegation = symbol.Type.GetAttributes()
				.Select(x => x.AttributeClass)
				.FilterNull()
				.Select(x => new AttributeName(x.Name))
				.Any(x => x.NameWithAttributeSuffix == nameof(FactoryAttribute));
			if (!isDelegation)
			{
				return null;
			}

			var methods = symbol.Type.GetMembers().OfType<IMethodSymbol>()
				.Select(m =>
				{
					var mm = m.DeclaringSyntaxReferences
						.Select(x => x.GetSyntax())
						.OfType<MethodDeclarationSyntax>()
						.FirstOrDefault();
					return mm is null ? null : ExtractAspect(mm, context);
				}).FilterNull().ToArray();

			return new PropertyAspect(methods);
		}
	}
}
