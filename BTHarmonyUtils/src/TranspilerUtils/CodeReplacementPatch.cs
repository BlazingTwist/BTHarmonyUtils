using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using BTHarmonyUtils.ILUtils;
using HarmonyLib;
using JetBrains.Annotations;

namespace BTHarmonyUtils.TranspilerUtils {

	/// <summary>
	/// Class that acts as an intermediary between user and transpiler
	/// </summary>
	[PublicAPI]
	public class CodeReplacementPatch {

		private readonly int expectedMatches;
		private readonly MethodInfo markerMethodInfo;
		private readonly List<CodeInstruction> insertSequence = new List<CodeInstruction>();
		private readonly List<CodeInstruction> prefixSequence = new List<CodeInstruction>();
		private readonly List<CodeInstruction> targetSequence = new List<CodeInstruction>();
		private readonly List<CodeInstruction> postfixSequence = new List<CodeInstruction>();

		/// <summary>
		/// A construct intended to generify Transpilers and make them more accessible.
		/// </summary>
		/// <param name="expectedMatches">Amount of matches that this Patch should encounter, or less than 0 for any amount</param>
		/// <param name="insertInstructionSequence">Sequence of instructions to insert at the matching locations</param>
		/// <param name="prefixInstructionSequence">Sequence of instructions that should occur before the replace-sequence</param>
		/// <param name="targetInstructionSequence">Sequence of instructions that should be removed / replaced with the insert-sequence</param>
		/// <param name="postfixInstructionSequence">Sequence of instructions that should occur after the replace-sequence</param>
		/// <exception cref="InvalidDataException">thrown when no prefix, replace and postfix sequence is specified -> cannot match for anything</exception>
		public CodeReplacementPatch(
				int expectedMatches = -1,
				IEnumerable<CodeInstruction> insertInstructionSequence = null,
				IEnumerable<CodeInstruction> prefixInstructionSequence = null,
				IEnumerable<CodeInstruction> targetInstructionSequence = null,
				IEnumerable<CodeInstruction> postfixInstructionSequence = null
		) {
			this.expectedMatches = expectedMatches;

			if (insertInstructionSequence != null) {
				insertSequence.AddRange(insertInstructionSequence);
			}

			if (prefixInstructionSequence != null) {
				prefixSequence.AddRange(prefixInstructionSequence);
			}

			if (targetInstructionSequence != null) {
				targetSequence.AddRange(targetInstructionSequence);
			}

			if (postfixInstructionSequence != null) {
				postfixSequence.AddRange(postfixInstructionSequence);
			}

			if (prefixSequence.Count == 0 && targetSequence.Count == 0 && postfixSequence.Count == 0) {
				throw new InvalidDataException("No matchers specified, cannot apply patch!");
			}
		}

		/// <summary>
		/// This class is intended to make working with Transpilers more accessible for anybody.
		/// The MarkerMethod may contain 0 or 1 insert/prefix/target/postfix -sequences.
		/// </summary>
		/// <param name="expectedMatches">Amount of matches that this Patch should encounter, or less than 0 for any amount</param>
		/// <param name="markerMethod">A method that is using TranspilerMarkers to define insert/prefix/target/postfix -sequences</param>
		/// <exception cref="InvalidDataException">thrown when a sequence isn't closed or there is no prefix, target and postfix sequence</exception>
		public CodeReplacementPatch(int expectedMatches, Expression<Action> markerMethod) {
			this.expectedMatches = expectedMatches;

			Dictionary<TranspilerMarkers.Markers, List<CodeInstruction>> sequenceDict = new Dictionary<TranspilerMarkers.Markers, List<CodeInstruction>>();
			TranspilerMarkers.Markers? currentSequence = null;
			markerMethodInfo = SymbolExtensions.GetMethodInfo(markerMethod);
			List<CodeInstruction> markerInstructions = new MethodBodyReader(markerMethodInfo).ReadInstructions();
			foreach (CodeInstruction markerInstruction in markerInstructions.Where(markerInstruction => markerInstruction.opcode != OpCodes.Nop)) {
				if (InstructionUtils.InstructionMatches(markerInstruction, TranspilerMarkers.ci_insertSequenceStart)) {
					currentSequence = TranspilerMarkers.Markers.insertSequence;
					sequenceDict.Add(TranspilerMarkers.Markers.insertSequence, insertSequence);
				} else if (InstructionUtils.InstructionMatches(markerInstruction, TranspilerMarkers.ci_prefixSequenceStart)) {
					currentSequence = TranspilerMarkers.Markers.prefixSequence;
					sequenceDict.Add(TranspilerMarkers.Markers.prefixSequence, prefixSequence);
				} else if (InstructionUtils.InstructionMatches(markerInstruction, TranspilerMarkers.ci_targetSequenceStart)) {
					currentSequence = TranspilerMarkers.Markers.targetSequence;
					sequenceDict.Add(TranspilerMarkers.Markers.targetSequence, targetSequence);
				} else if (InstructionUtils.InstructionMatches(markerInstruction, TranspilerMarkers.ci_postfixSequenceStart)) {
					currentSequence = TranspilerMarkers.Markers.postfixSequence;
					sequenceDict.Add(TranspilerMarkers.Markers.postfixSequence, postfixSequence);
				} else if (InstructionUtils.InstructionMatches(markerInstruction, TranspilerMarkers.ci_LastSequenceEnd)) {
					currentSequence = null;
				} else {
					if (currentSequence == null) {
						continue;
					}
					sequenceDict[currentSequence.Value].Add(markerInstruction);
				}
			}

			if (currentSequence != null) {
				throw new InvalidDataException($"MarkerMethod is missing {nameof(TranspilerMarkers.LastSequenceEnd)} marker!");
			}

			if (prefixSequence.Count == 0 && targetSequence.Count == 0 && postfixSequence.Count == 0) {
				throw new InvalidDataException("No matchers specified, cannot apply patch!");
			}
		}

		/// <summary>
		/// Move the labels in the replace-sequence and first label of postfix-sequence to beginning of insert-sequence
		/// </summary>
		/// <returns>a new insert-sequence with adjusted labels</returns>
		private List<CodeInstruction> MoveLabels(
				List<CodeInstruction> instructions,
				int index,
				int insertLength,
				int prefixLength,
				int replaceLength,
				int postfixLength
		) {
			int instructionCount = instructions.Count;
			List<Label> allLabels = replaceLength > 0
					? InstructionUtils.FindAllLabels(instructions, index + prefixLength, index + prefixLength + replaceLength)
					: new List<Label>();

			// when there is no insert-sequence, move labels of replace-sequence to start of postfix sequence
			if (insertLength == 0) {
				// make sure there actually is an instruction we can attach these labels to
				int labelIndex = index + prefixLength + replaceLength;
				if (labelIndex < instructionCount) {
					instructions[labelIndex].labels.AddRange(allLabels);
					return new List<CodeInstruction>();
				}

				// otherwise insert a nop instruction to take all the labels
				CodeInstruction nopInst = new CodeInstruction(OpCodes.Nop);
				nopInst.labels.AddRange(allLabels);
				return new List<CodeInstruction> { nopInst };
			}

			// insert-sequence provided, move label of first postfix instruction as well.
			if (postfixLength > 0) {
				CodeInstruction firstPostfixInstruction = instructions[index + prefixLength + replaceLength];
				allLabels.AddRange(firstPostfixInstruction.labels);
				firstPostfixInstruction.labels.Clear();
			}

			CodeInstruction newFirstInstruction = new CodeInstruction(insertSequence[0]);
			newFirstInstruction.labels.AddRange(allLabels);
			List<CodeInstruction> newInsertSequence = new List<CodeInstruction> { newFirstInstruction };
			newInsertSequence.AddRange(insertSequence.GetRange(1, insertLength - 1));
			return newInsertSequence;
		}

		/// <summary>
		/// Apply this ReplacementPatch to the given instructions
		/// </summary>
		/// <param name="instructions">instructions to apply the changes to</param>
		/// <exception cref="InvalidDataException">thrown when matchers find an unexpected amount of matches or the matches are overlapping</exception>
		public void Apply(List<CodeInstruction> instructions) {
			int insertLength = insertSequence.Count;
			int prefixLength = prefixSequence.Count;
			int replaceLength = targetSequence.Count;
			int postfixLength = postfixSequence.Count;
			List<int> sequenceMatches = null;

			if (prefixLength > 0) {
				sequenceMatches = InstructionUtils.FindInstructionSequence(instructions, prefixSequence);
			}

			if (replaceLength > 0) {
				sequenceMatches = sequenceMatches != null
						? sequenceMatches
								.Where(index => InstructionUtils.SequenceMatches(instructions, targetSequence, index + prefixLength))
								.ToList()
						: InstructionUtils.FindInstructionSequence(instructions, targetSequence);
			}

			if (postfixLength > 0) {
				sequenceMatches = sequenceMatches != null
						? sequenceMatches
								.Where(index => InstructionUtils.SequenceMatches(instructions, postfixSequence, index + prefixLength + replaceLength))
								.ToList()
						: InstructionUtils.FindInstructionSequence(instructions, postfixSequence);
			}

			sequenceMatches = sequenceMatches ?? new List<int>();
			int sequenceMatchesCount = sequenceMatches.Count;

			if (expectedMatches >= 0 && sequenceMatchesCount != expectedMatches) {
				throw new InvalidDataException(
						$"CodeReplacementPatch has found {sequenceMatchesCount} matches, but expected to find {expectedMatches}! Mod may be outdated!");
			}

			// verify that every index is at least `replaceLength` many elements apart -> same code isn't replaced twice
			if (sequenceMatchesCount > 0) {
				int previousIndex = sequenceMatches[0];
				for (int i = 1; i < sequenceMatchesCount; i++) {
					int index = sequenceMatches[i];
					if (index - previousIndex < replaceLength) {
						throw new InvalidDataException("CodeReplacementPatch has matched overlapping replaceSequences! Mod may be outdated!");
					}
				}
			}

			// iterate over indexes in reverse, this way we don't have to update the indexes after every removal / insertion
			for (int i = sequenceMatchesCount - 1; i >= 0; i--) {
				int index = sequenceMatches[i];
				List<CodeInstruction> insertSequenceWithLabels = MoveLabels(instructions, index, insertLength, prefixLength, replaceLength, postfixLength);
				instructions.InsertRange(index + prefixLength + replaceLength, insertSequenceWithLabels);
				instructions.RemoveRange(index + prefixLength, replaceLength);
			}
		}

		/// <summary>
		/// Apply this ReplacementPatch to the given instructions
		/// Catches all exceptions and writes them to the logger
		/// </summary>
		/// <param name="instructions">instructions to apply the changes to</param>
		/// <param name="logger">logger to write exceptions to</param>
		public void ApplySafe(List<CodeInstruction> instructions, ManualLogSource logger) {
			try {
				Apply(instructions);
			} catch (InvalidDataException e) {
				if (markerMethodInfo != null) {
					logger.LogError($"Patching with MarkerMethod {markerMethodInfo.Name} caused error: {e.Message}\n{e.StackTrace}");
				} else {
					string calleeName = new StackFrame(1).GetMethod().Name;
					logger.LogError($"Patching {calleeName} caused error: {e.Message}\n{e.StackTrace}");
				}
			}
		}

	}

}
