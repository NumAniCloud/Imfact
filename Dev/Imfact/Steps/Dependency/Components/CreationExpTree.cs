﻿using System.Linq;
using Imfact.Entities;
using Imfact.Utilities;

namespace Imfact.Steps.Dependency.Components;

internal record CreationExpTree(ICreationNode Root);

internal record MultiCreationExpTree(ICreationNode[] Roots);

internal interface ICreationNode
{
	string Code { get; }
}

internal record Invocation(string AccessPart, ICreationNode[] Arguments) : ICreationNode
{
	public string Code => $"{AccessPart}({GetArgList()})";

	private string GetArgList()
	{
		return Arguments.Select(x => x.Code).Join(", ");
	}
}

internal record Variable(string AccessExp) : ICreationNode
{
	public string Code => AccessExp;
}

internal record UnsatisfiedField(TypeAnalysis Type, string Name) : ICreationNode
{
	public string Code => Name;
}