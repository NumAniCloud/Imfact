﻿using System.Collections.Generic;
using System.Linq;
using Deptorygen2.Core.Interfaces;
using Deptorygen2.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NacHelpers.Extensions;

namespace Deptorygen2.Core.Steps.Aspects.Nodes
{
	internal record Class(ClassDeclarationSyntax Syntax, INamedTypeSymbol Symbol)
	{
		public bool IsFactory()
		{
			return Syntax.AttributeLists.HasAttribute(new AttributeName("FactoryAttribute"))
				&& Syntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
		}

		public Class[] GetBaseClasses(IAnalysisContext context)
		{
			IEnumerable<INamedTypeSymbol> Traverse(INamedTypeSymbol pivot)
			{
				if (pivot.BaseType is not null)
				{
					yield return pivot.BaseType;
					foreach (var symbol in Traverse(pivot.BaseType))
					{
						yield return symbol;
					}
				}
			}

			// 果たしてここのGetSyntaxは上手くいくか？
			return Traverse(Symbol).Select(x =>
			{
				var syntax = x.DeclaringSyntaxReferences.Select(y => y.GetSyntax())
					.OfType<ClassDeclarationSyntax>()
					.FirstOrDefault();
				if (syntax is null)
				{
					return null;
				}

				return new Class(syntax, x);
			}).FilterNull().ToArray();
		}

		public Method[] GetMethods(IAnalysisContext context)
		{
			return Syntax.Members
				.OfType<MethodDeclarationSyntax>()
				.Select(m => context.GetMethodSymbol(m) is { } symbol
					? new Method(m, symbol)
					: null)
				.FilterNull()
				.ToArray();
		}

		public Property[] GetProperties(IAnalysisContext context)
		{
			return Syntax.Members
				.OfType<PropertyDeclarationSyntax>()
				.Select(m => context.GetPropertySymbol(m) is { } symbol
					? new Property(m, symbol)
					: null)
				.FilterNull()
				.ToArray();
		}
	}
}