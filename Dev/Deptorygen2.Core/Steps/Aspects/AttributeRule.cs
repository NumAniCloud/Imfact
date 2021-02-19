﻿using System;
using Deptorygen2.Annotations;
using Deptorygen2.Core.Entities;
using Deptorygen2.Core.Utilities;
using Microsoft.CodeAnalysis;

namespace Deptorygen2.Core.Steps.Aspects
{
	internal class AttributeReceptor
	{
		private readonly AspectRule _originalxx;
		private readonly AttributeName _resAt = new(nameof(ResolutionAttribute));
		private readonly AttributeName _hokAt = new(nameof(HookAttribute));
		private readonly AttributeName _cacAt = new(nameof(CacheAttribute));
		private readonly AttributeName _cprAt = new(nameof(CachePerResolutionAttribute));

		public AttributeReceptor(AspectRule originalxx)
		{
			_originalxx = originalxx;
		}

		public MethodAttributeAspect? ExtractAspect(AttributeData data,
			INamedTypeSymbol ownerReturn,
			string ownerName)
		{
			if (data.AttributeClass?.Name is not { } name)
			{
				return null;
			}

			AnnotationKind kind;
			TypeToCreate? type;

			if (_resAt.MatchWithAnyName(name))
			{
				if (Resolution(data, ownerReturn) is not { } tuple)
				{
					return null;
				}

				(kind, type) = tuple;
			}
			else if (_hokAt.MatchWithAnyName(name))
			{
				if (Hook(data, ownerReturn) is not {} tuple)
				{
					return null;
				}

				(kind, type) = tuple;
			}
			else if (_cacAt.MatchWithAnyName(name))
			{
				kind = AnnotationKind.CacheHookPreset;
				type = PresetCache(typeof(Cache<>), ownerReturn);
			}
			else if (_cprAt.MatchWithAnyName(name))
			{
				kind = AnnotationKind.CachePrHookPreset;
				type = PresetCache(typeof(CachePerResolution<>), ownerReturn);
			}
			else
			{
				return null;
			}

			return new MethodAttributeAspect(kind, TypeNode.FromSymbol(ownerReturn), ownerName, type);
		}

		private (AnnotationKind, TypeToCreate)? Resolution(AttributeData data,
			INamedTypeSymbol ownerReturn)
		{
			if (data.ConstructorArguments.Length == 1
			    && data.ConstructorArguments[0].Kind == TypedConstantKind.Type
			    && data.ConstructorArguments[0].Value is INamedTypeSymbol t)
			{
				var kind = AnnotationKind.Resolution;
				var type = _originalxx.ExtractTypeToCreate(t, ownerReturn);
				return (kind, type);
			}

			return null;
		}

		private (AnnotationKind, TypeToCreate)? Hook(AttributeData data,
			INamedTypeSymbol ownerReturn)
		{
			if (data.ConstructorArguments.Length == 1
			    && data.ConstructorArguments[0].Kind == TypedConstantKind.Type
			    && data.ConstructorArguments[0].Value is INamedTypeSymbol arg
			    && arg.ConstructedFrom.IsImplementing(typeof(IHook<>)))
			{
				var kind = AnnotationKind.Hook;
				var type = _originalxx.ExtractTypeToCreate(arg, ownerReturn);
				return (kind, type);
			}

			return null;
		}

		private static TypeToCreate PresetCache(Type hookType, INamedTypeSymbol ownerReturn)
		{
			var node = TypeNode.FromRuntime(hookType,
				new[] {TypeNode.FromSymbol(ownerReturn)});
			return new TypeToCreate(node, new TypeNode[0]);
		}
	}
}
