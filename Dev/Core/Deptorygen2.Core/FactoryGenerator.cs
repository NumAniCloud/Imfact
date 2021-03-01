﻿using System.Collections.Generic;
using System.Text;
using Deptorygen2.Core.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Deptorygen2.Core
{
	[Generator]
	public class FactoryGenerator : ISourceGenerator
	{
		public void Initialize(GeneratorInitializationContext context)
		{
			context.RegisterForSyntaxNotifications(() => new FactorySyntaxReceiver());
		}

		public void Execute(GeneratorExecutionContext context)
		{
			//System.Diagnostics.Debugger.Launch();
			AnnotationGenerator.AddSource(in context);

			if (context.SyntaxReceiver is not FactorySyntaxReceiver receiver
				|| receiver.SyntaxTree is null)
			{
				return;
			}

			var options =
				(CSharpParseOptions) ((CSharpCompilation) context.Compilation).SyntaxTrees[0]
				.Options;
			var compilation =
				context.Compilation.AddSyntaxTrees(AnnotationGenerator.GetSyntaxTrees(options));

			var semanticModel = compilation.GetSemanticModel(receiver.SyntaxTree);
			var facade = new GenerationFacade(semanticModel);

			var sourceFiles = facade.Run(receiver.CandidateClasses.ToArray());
			foreach (var file in sourceFiles)
			{
				var sourceText = SourceText.From(file.Contents, Encoding.UTF8);
				context.AddSource(file.FileName, sourceText);
			}
		}
	}

	class FactorySyntaxReceiver : ISyntaxReceiver
	{
		public SyntaxTree? SyntaxTree { get; set; }
		public List<ClassDeclarationSyntax> CandidateClasses { get; } = new();

		public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
		{
			SyntaxTree ??= syntaxNode.SyntaxTree;

			if (syntaxNode is ClassDeclarationSyntax classDeclarationSyntax
				&& classDeclarationSyntax.AttributeLists.Count > 0)
			{
				CandidateClasses.Add(classDeclarationSyntax);
			}
		}
	}
}
