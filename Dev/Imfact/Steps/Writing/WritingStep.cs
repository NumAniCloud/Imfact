﻿using System.Collections.Generic;
using System.Linq;
using Imfact.Steps.Definitions;
using Imfact.Steps.Definitions.Interfaces;
using Imfact.Steps.Definitions.Methods;
using Imfact.Steps.Writing.Coding;
using Imfact.Utilities;

namespace Imfact.Steps.Writing
{
	internal class WritingStep
	{
		private readonly DefinitionRoot _definitionRoot;
		private readonly ResolverWriter _resolverWriter;

		public WritingStep(DefinitionResult definitionStepResult)
		{
			_definitionRoot = definitionStepResult.DefinitionRoot;
			_resolverWriter = new ResolverWriter();
		}

		public SourceFile Write()
		{
			var fileName = _definitionRoot.Namespace.Class.Name + ".g";
			var contents = Render();
			return new SourceFile(fileName, contents);
		}

		private string Render()
		{
			var builder = CodeHelper.GetBuilder();

			builder.AppendLine("// <autogenerated />");
			builder.AppendLine("#nullable enable");

			foreach (var usingNode in _definitionRoot.Usings)
			{
				builder.AppendLine($"using {usingNode.Namespace};");
			}

			builder.AppendLine();

			builder.AppendLine($"namespace {_definitionRoot.Namespace.Name}");
			builder.EnterBlock(inner =>
			{
				RenderClass(_definitionRoot.Namespace.Class, inner);
			});

			return builder.GetText();
		}

		private void RenderClass(Class @class, ICodeBuilder builder)
		{
			RenderClassSignature(@class, builder);
			builder.EnterBlock(block =>
			{
				block.EnterSequence(seqOuter =>
				{
					seqOuter.EnterChunk(chunk =>
					{
						foreach (var field in @class.Fields)
						{
							var ro = field.IsReadonly ? "readonly " : "";
							var access = field.Accessibility.ToKeyword();
							chunk.AppendLine($"{access} {ro}{field.TypeAnalysis.GetCode()} {field.Name};");
						}
					});

					foreach (var method in @class.Methods)
					{
						RenderMethod(method, seqOuter);
					}

					foreach (var exporter in @class.Exporters)
					{
						RenderExporter(exporter, seqOuter);
					}
				});
			});
		}

		private void RenderExporter(Exporter exporter, ICodeBuilder builder)
		{
			builder.EnterChunk(chunk =>
			{
				var p = exporter.Parameters[0];

				var param0 = $"{p.TypeAnalysis.FullNamespace}.{p.TypeAnalysis.Name} {p.Name}";
				var signature = $"public void {exporter.Name}({param0})";
				chunk.AppendLine(signature);

				chunk.EnterBlock(block =>
				{
					foreach (var item in exporter.Items)
					{
						block.AppendLine($"{exporter.Name}<{item.InterfaceType.Name}, {item.ConcreteType.Name}>({p.Name}, () => {item.MethodName}());");
					}
				});
			});
		}

		private void RenderClassSignature(Class @class, ICodeBuilder builder)
		{
			builder.Append($"partial class {@class.Name}");

			var interfaces = GetInterfaces(@class).ToArray();
			var baseTypeList = interfaces.Any()
				? " : " + interfaces.Join(", ")
				: "";

			builder.AppendLine(baseTypeList);
		}

		private IEnumerable<string> GetInterfaces(Class @class)
		{
			if (@class.DisposableInfo.HasDisposable)
			{
				yield return "IDisposable";
			}

			if (@class.DisposableInfo.HasAsyncDisposable)
			{
				yield return "IAsyncDisposable";
			}
		}

		private void RenderMethod(MethodInfo method, ICodeBuilder builder)
		{
			builder.EnterChunk(chunk =>
			{
				chunk.AppendLine(method.Signature.GetSignatureString());
				chunk.EnterBlock(block =>
				{
					var fluent = new FluentCodeBuilder(block);
					method.Implementation.Render(fluent, _resolverWriter);
				});
			});
		}
	}
}
