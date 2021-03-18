﻿using System.Linq;
using Imfact.Steps.Semanticses.Interfaces;
using Imfact.Steps.Semanticses.Records;
using Imfact.Utilities;
using Microsoft.CodeAnalysis;

namespace Imfact.Steps.Dependency.Interfaces
{
	interface IFactorySource<T> where T : IFactorySemantics
	{
		string GetVariableName(T source);
		T[] GetDelegationSource();
		bool IsAvailable<TResolver>(TResolver source) where TResolver : IResolverSemantics;
	}

	class RootFactorySource : IFactorySource<Factory>
	{
		private readonly SemanticsRoot _semantics;

		public RootFactorySource(SemanticsRoot semantics)
		{
			this._semantics = semantics;
		}

		public string GetVariableName(Factory source) => "this";

		public Factory[] GetDelegationSource()
			=> _semantics.Factory.WrapByArray();

		public bool IsAvailable<TResolver>(TResolver source) where TResolver : IResolverSemantics
		{
			return true;
		}
	}

	class DelegationSource : IFactorySource<Delegation>
	{
		private static readonly Accessibility[] AvailableLevels = new[]
		{
			Accessibility.Public,
			Accessibility.Internal,
			Accessibility.ProtectedOrInternal
		};

		private readonly SemanticsRoot _semantics;

		public DelegationSource(SemanticsRoot semantics)
		{
			this._semantics = semantics;
		}

		public string GetVariableName(Delegation source) => source.PropertyName;
		public Delegation[] GetDelegationSource()
			=> _semantics.Factory.Delegations;

		public bool IsAvailable<TResolver>(TResolver source)
			where TResolver : IResolverSemantics
		{
			return AvailableLevels.Contains(source.Accessibility);
		}
	}

	class InheritanceSource : IFactorySource<Inheritance>
	{
		private static readonly Accessibility[] AvailableLevels = new[]
		{
			Accessibility.Public,
			Accessibility.Internal,
			Accessibility.ProtectedOrInternal,
			Accessibility.Protected,
			Accessibility.ProtectedAndInternal,
		};

		private readonly SemanticsRoot _semantics;

		public InheritanceSource(SemanticsRoot semantics)
		{
			this._semantics = semantics;
		}

		public string GetVariableName(Inheritance source) => "base";

		public Inheritance[] GetDelegationSource()
			=> _semantics.Factory.Inheritances;

		public bool IsAvailable<TResolver>(TResolver source)
			where TResolver : IResolverSemantics
		{
			return AvailableLevels.Contains(source.Accessibility);
		}
	}
}