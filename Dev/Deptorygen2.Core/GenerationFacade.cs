﻿using Deptorygen2.Core.Interfaces;
using Deptorygen2.Core.Steps.Aggregation;
using Deptorygen2.Core.Steps.Api;
using Deptorygen2.Core.Steps.Definitions;
using Deptorygen2.Core.Steps.Definitions.Syntaxes;
using Deptorygen2.Core.Steps.Semanticses;
using Deptorygen2.Core.Steps.Writing;
using Deptorygen2.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Deptorygen2.Core
{
	public class GenerationFacade
	{
		private readonly IAnalysisContext _context;
		private readonly AspectAggregator _aspectAggregator = new();
		private readonly SemanticsAggregator _semanticsAggregator;

		public GenerationFacade(SemanticModel semanticModel)
		{
			_context = new CompilationAnalysisContext(semanticModel);
			_semanticsAggregator = new SemanticsAggregator(_context);
		}

		public SourceFile? RunGeneration(ClassDeclarationSyntax syntax)
		{
			return AspectStep(syntax) is not { } aspect ? null
				: SemanticsStep(aspect) is not { } semantics ? null
				: SourceCodeStep(DefinitionStep(semantics));
		}

		private SyntaxOnAspect? AspectStep(ClassDeclarationSyntax syntax)
		{
			return _aspectAggregator.Aggregate(syntax, _context) is { } aspect
				? new SyntaxOnAspect(aspect) : null;
		}

		private DeptorygenSemantics? SemanticsStep(SyntaxOnAspect aspect)
		{
			return _semanticsAggregator.Aggregate(aspect.Class, _context) is { } semantics
				? new DeptorygenSemantics(semantics) : null;
		}

		private SourceTreeDefinition DefinitionStep(DeptorygenSemantics semantics)
		{
			var builder = new DefinitionTreeBuilder(semantics.Semantics);
			return builder.Build();
		}

		private SourceFile SourceCodeStep(SourceTreeDefinition definition)
		{
			var writer = new SourceCodeBuilder(definition);
			return writer.Write();
		}
	}
}