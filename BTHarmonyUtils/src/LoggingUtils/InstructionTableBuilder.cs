using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;

namespace BTHarmonyUtils.LoggingUtils {

	/// <summary>
	/// Useful for logging Instructions in a readable format.
	/// </summary>
	[PublicAPI]
	public static class InstructionTableBuilder {

		/// <summary>
		/// Builds a table containing a list of instructions.
		/// </summary>
		/// <param name="instructions">The instructions for the table</param>
		/// <returns>The String representation of the table.</returns>
		public static string BuildTable(IEnumerable<CodeInstruction> instructions) {
			TextTableBuilder tableBuilder = new TextTableBuilder();
			tableBuilder.Row("Labels", "OpCode", "Operand");
			tableBuilder.ThinRowSeparator();

			foreach (CodeInstruction instruction in instructions) {
				if (instruction.labels.Count > 5) {
					List<List<Label>> labelBatches = GetAsBatches(instruction.labels, 5);
					tableBuilder.ThinRowSeparator();
					tableBuilder.Row(LabelsToString(labelBatches[0]), instruction.opcode.ToString(), OperandToString(instruction.operand));
					for (int i = 1; i < labelBatches.Count; i++) {
						tableBuilder.Row(LabelsToString(labelBatches[i]));
					}
					tableBuilder.ThinRowSeparator();
				} else {
					tableBuilder.Row(LabelsToString(instruction.labels), instruction.opcode.ToString(), OperandToString(instruction.operand));
				}
			}

			tableBuilder.EndTable();
			return tableBuilder.BuildTable("  ");
		}

		private static string LabelsToString(IEnumerable<Label> labels) {
			return string.Join(", ", labels.Select(label => label.GetHashCode()));
		}

		private static List<List<T>> GetAsBatches<T>(IReadOnlyList<T> enumerable, int batchSize) {
			List<List<T>> batches = new List<List<T>>();

			int enumerableCount = enumerable.Count;
			for (int i = 0; i < enumerableCount; i += batchSize) {
				List<T> batch = new List<T>();
				int numAvailableItems = Math.Min(batchSize, enumerableCount - i);
				for (int batchIndex = 0; batchIndex < numAvailableItems; batchIndex++) {
					batch.Add(enumerable[i + batchIndex]);
				}
				batches.Add(batch);
			}

			return batches;
		}

		private static string OperandToString(object operand) {
			if (operand == null) {
				return "null";
			}
			if (operand is LocalBuilder localBuilder) {
				return $"{localBuilder.LocalIndex} ({localBuilder.LocalType})";
			}
			if (operand is Label label) {
				return label.GetHashCode().ToString();
			}
			return operand.ToString();
		}

	}

}
