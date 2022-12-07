﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Imfact.Entities;
using Imfact.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Imfact.Steps.Aspects.Rules
{
    internal sealed class MethodRule
	{
		private readonly AttributeRule _attributeRule;
		private readonly TypeRule _typeRule;

		public MethodRule(AttributeRule attributeRule,
			TypeRule typeRule)
		{
			_attributeRule = attributeRule;
			_typeRule = typeRule;
		}

		public MethodAspect? ExtractAspect(MethodDeclarationSyntax syntax, IMethodSymbol symbol, bool partialOnly = false)
		{
			// TODO: partial判定が済んでいる情報が送られてくる前提の実装にする
			using var profiler = TimeProfiler.Create("Extract-Method-Aspect");
			if (partialOnly && !IsResolverPartial(syntax)
				|| symbol.ReturnType is not INamedTypeSymbol returnSymbol)
			{
				return null;
			}

			return new MethodAspect(symbol.Name,
				symbol.DeclaredAccessibility,
				GetKind(returnSymbol),
				GetReturnType(returnSymbol),
				GetAttributes(symbol, returnSymbol),
				GetParameters(symbol));
		}

		public MethodAspect? ExtractNotAsRootResolver(IMethodSymbol symbol)
		{
			if (symbol.ReturnType is not INamedTypeSymbol returnSymbol)
			{
				return null;
			}

			return new MethodAspect(symbol.Name,
				symbol.DeclaredAccessibility,
				GetKind(returnSymbol),
				GetReturnType(returnSymbol),
				GetAttributes(symbol, returnSymbol),
				GetParameters(symbol));
		}

		private ParameterAspect[] GetParameters(IMethodSymbol symbol)
		{
			return symbol.Parameters
				.Select(p => new ParameterAspect(TypeAnalysis.FromSymbol(p.Type), p.Name))
				.ToArray();
		}

		private MethodAttributeAspect[] GetAttributes(IMethodSymbol symbol, INamedTypeSymbol returnSymbol)
		{
			using var profiler = TimeProfiler.Create("Extract-Attribute-Aspect");
			return symbol.GetAttributes()
				.Select(x => _attributeRule.ExtractAspect(x, returnSymbol, symbol.Name))
				.FilterNull()
				.ToArray();
		}

		private ReturnTypeAspect GetReturnType(INamedTypeSymbol symbol)
		{
			using var profiler = TimeProfiler.Create("Extract-ReturnType-Aspect");
			return new(_typeRule.ExtractTypeToCreate(symbol), symbol.IsAbstract);
		}

		private static ResolverKind GetKind(INamedTypeSymbol returnSymbol)
		{
			var idSymbol = returnSymbol.ConstructedFrom;
			var typeNameValid = idSymbol.MetadataName == typeof(IEnumerable<>).Name;
			var typeArgValid = idSymbol.TypeArguments.Length == 1;
			return typeNameValid && typeArgValid ? ResolverKind.Multi : ResolverKind.Single;
		}

		private static bool IsResolverPartial(MethodDeclarationSyntax syntax)
		{
			var hasAccessor = syntax.Modifiers.Any(x =>
				x.IsKind(SyntaxKind.PublicKeyword)
				|| x.IsKind(SyntaxKind.PrivateKeyword)
				|| x.IsKind(SyntaxKind.ProtectedKeyword)
				|| x.IsKind(SyntaxKind.InternalKeyword));

			return syntax.Modifiers.Any(x => x.IsKind(SyntaxKind.PartialKeyword))
				&& hasAccessor;
		}
	}
}
