﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Imfact.Entities;
using Imfact.Incremental;
using Imfact.Main;
using Imfact.Steps.Ranking;
using Imfact.Utilities;
using Microsoft.CodeAnalysis;

namespace Imfact.Steps.Aspects.Rules;

internal class ClassRule
{
	private readonly GenerationContext _genContext;
	private readonly MethodRule _methodRule;
	private readonly PropertyRule _propertyRule;
	private readonly FactoryCandidate[] _factoryCandidates;

	public ClassRule
		(GenerationContext genContext, MethodRule methodRule,
		PropertyRule propertyRule, FactoryCandidate[] factoryCandidates)
	{
		_genContext = genContext;
		_methodRule = methodRule;
		_propertyRule = propertyRule;
		_factoryCandidates = factoryCandidates;
	}

	public ClassAspect Aggregate(RankedClass root)
	{
		return ExtractThis(root.Self, ExtractBases(root.Self));
	}

	private ClassAspect ExtractThis(FactoryCandidate factory, ClassAspect[] baseClasses)
	{
		var symbol = factory.Symbol;
		return new(TypeAnalysis.FromSymbol(symbol),
			baseClasses,
			ExtractInterfaces(symbol),
			GetMethodAspects(factory, partialOnly: true),
			GetPropertyAspects(symbol),
			null);
	}

    private ClassAspect[] ExtractBases(FactoryCandidate factory)
	{
		// 基底クラスのコンストラクタを呼ぶためには基底クラスを通す必要がある
		// 基底クラスのリゾルバーを呼ぶためには基底クラスを通す必要がある
		return TraverseBase(factory.Symbol)
			.Select(x => new FactoryCandidate(x, GetMethods(x), factory.Context))
			.Select(ExtractBase)
			.ToArray();

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

		static ResolverCandidate[] GetMethods(INamedTypeSymbol holder)
		{
			return holder.GetMembers()
				.OfType<IMethodSymbol>()
				.Select(x => new ResolverCandidate(x, false))
				.ToArray();
		}
	}

	// NOTE: 基底クラスのプロパティを追う必要は無いのでは？
	// 逆に、コンストラクタはこちらにしか無いので、BaseClassAspect みたいな型で区別してもいいかも
	private ClassAspect ExtractBase(FactoryCandidate factory)
	{
		var symbol = factory.Symbol;
		return new(
			TypeAnalysis.FromSymbol(symbol),
			new ClassAspect[0],
			ExtractInterfaces(symbol),
			GetMethodAspects(factory, false),
			GetPropertyAspects(symbol),
			GetConstructor(symbol));
	}

	private InterfaceAspect[] ExtractInterfaces(INamedTypeSymbol thisSymbol)
	{
		return thisSymbol.AllInterfaces
			.Select(x => new InterfaceAspect(TypeAnalysis.FromSymbol(x)))
			.ToArray();
	}

	private MethodAspect[] GetMethodAspects(FactoryCandidate factory, bool partialOnly)
	{
		var members = partialOnly
			? factory.Methods.Where(x => x.IsToGenerate)
			: factory.Methods;

		return members
			.Select(x => x.Symbol)
			.Select(_methodRule.ExtractAspect)
			.FilterNull()
			.ToArray();
	}

	private PropertyAspect[] GetPropertyAspects(INamedTypeSymbol @class)
	{
		return @class.GetMembers()
			.OfType<IPropertySymbol>()
			.Select(_propertyRule.ExtractAspect)
			.FilterNull()
			.ToArray();
	}

	private ConstructorAspect GetConstructor(INamedTypeSymbol symbol)
	{
		var ctor = symbol.Constructors.MaxItem(x => x.Arity);

		var parameters = ctor.Parameters
			.Select(x => new ParameterAspect(TypeAnalysis.FromSymbol(x.Type), x.Name))
			.ToArray();

		return new ConstructorAspect(ctor.DeclaredAccessibility, parameters);
	}
}