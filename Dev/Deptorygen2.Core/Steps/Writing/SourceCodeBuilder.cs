﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Deptorygen2.Core.Steps.Creation;
using Deptorygen2.Core.Steps.Definitions.Syntaxes;
using NacHelpers.Extensions;

namespace Deptorygen2.Core.Steps.Writing
{
	internal class SourceCodeBuilder
	{
		private readonly RootNode _root;
		private readonly ICreationAggregator _creation;
		private readonly ResolverWriter _resolverWriter;

		public SourceCodeBuilder(SourceTreeDefinition definition)
		{
			_root = definition.Root;
			_creation = definition.Creation;
			_resolverWriter = new ResolverWriter();
		}

		public SourceFile Write()
		{
			var fileName = _root.Namespace.Class.Name + ".g";
			var contents = Render();
			return new SourceFile(fileName, contents);
		}

		private string Render()
		{
			var builder = new StringBuilder();

			builder.AppendLine("// <autogenerated />");
			builder.AppendLine("#nullable enable");

			foreach (var usingNode in _root.Usings)
			{
				builder.AppendLine($"using {usingNode.Namespace};");
			}

			builder.AppendLine();

			AppendBlock(builder, $"namespace {_root.Namespace.Name}", inner =>
			{
				RenderClass(_root.Namespace.Class, inner);
			});

			return builder.ToString();
		}

		private void RenderClass(ClassNode @class, StringBuilder builder)
		{
			AppendBlock(builder, $"partial class {@class.Name}", inner =>
			{
				foreach (var field in @class.Fields)
				{
					inner.AppendLine($"private readonly {field.Type.Text} {field.Name};");
				}

				inner.AppendLine();

				RenderConstructor(@class.Constructor, inner);

				inner.AppendLine();

				AppendSequence(@class.Methods, inner, 
					method => RenderMethod(method, inner));
			});
		}

		private void AppendSequence<T>(IEnumerable<T> collection, StringBuilder builder, Action<T> renderer)
		{
			foreach (var item in collection.WithIndex())
			{
				if (item.index != 0)
				{
					builder.AppendLine();
				}
				renderer(item.item);
			}
		}

		private void RenderMethod(MethodNode method, StringBuilder builder)
		{
			var paramList = method.Parameters
				.Select(x => $"{x.Type.Text} {x.Name}")
				.Join(", ");

			var access = method.Accessibility.ToString().ToLower();
			var ret = method.ReturnType.Text;

			AppendBlock(builder, $"{access} partial {ret} {method.Name}({paramList})", inner =>
			{
				_resolverWriter.RenderImplementation(method, _creation, inner);
			});
		}

		private void RenderConstructor(ConstructorNode ctor, StringBuilder builder)
		{
			var paramList = ctor.Parameters
				.Select(x => $"{x.Type.Text} {x.Name}")
				.Join(", ");

			AppendBlock(builder, $"public {ctor.Name}({paramList})", inner =>
			{
				foreach (var assignment in ctor.Assignments)
				{
					inner.AppendLine($"{assignment.Dest} = {assignment.Src};");
				}
			});
		}

		public static void AppendBlock(StringBuilder builder, string header, Action<StringBuilder> build)
		{
			builder.AppendLine(header);
			builder.AppendLine("{");

			var innerBuilder = new StringBuilder();
			build(innerBuilder);

			builder.AppendLine(innerBuilder.ToString().Indent(1).TrimEnd());
			builder.AppendLine("}");
		}
	}
}
