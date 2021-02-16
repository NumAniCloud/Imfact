﻿using System;
using System.Linq;
using System.Text.RegularExpressions;
using Deptorygen2.Core.Utilities;
using Microsoft.CodeAnalysis;

namespace Deptorygen2.Core.Entities
{
	internal record TypeRecord(
		string FullNamespace, string Name, TypeRecord[] TypeArguments)
	{
		public static TypeRecord FromSymbol(INamedTypeSymbol symbol)
		{
			var typeArguments = symbol.TypeArguments
				.Select(FromSymbol)
				.ToArray();

			return new (symbol.GetFullNameSpace(), symbol.Name, typeArguments);
		}

		public static TypeRecord FromSymbol(ITypeSymbol symbol)
		{
			return symbol is INamedTypeSymbol nts
				? FromSymbol(nts)
				: throw new ArgumentException(nameof(symbol));
		}

		public static TypeRecord FromRuntime(Type type, TypeRecord[]? typeArguments = null)
		{
			return new(
				type.Namespace ?? "",
				Regex.Replace(type.Name, @"`\d+$", ""),
				typeArguments ?? new TypeRecord[0]);
		}
	}
}
