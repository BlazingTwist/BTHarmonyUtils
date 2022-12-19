using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using BTHarmonyUtils.ILUtils;
using HarmonyLib;
using JetBrains.Annotations;

namespace BTHarmonyUtils.InstructionSearch {

	/// <summary>
	/// InstructionSearcher lets you look for CodeInstructions
	/// especially useful to access local variables or compiler generated code in transpilers
	/// </summary>
	[PublicAPI]
	public class InstructionSearcher {

		private readonly int expectedMatches;
		private readonly List<SearchMask> searchMasks = new List<SearchMask>();

		/// <summary>
		/// InstructionSearcher lets you look for CodeInstructions
		/// especially useful to access local variables or compiler generated code in transpilers
		/// </summary>
		/// <param name="searchMasks">Sequence of SearchMasks to search for</param>
		/// <param name="expectedMatches">Amount of matches this search should encounter, or less than 0 for any amount</param>
		public InstructionSearcher(IEnumerable<SearchMask> searchMasks, int expectedMatches = -1) {
			this.searchMasks.AddRange(searchMasks);
			this.expectedMatches = expectedMatches;
		}

		/// <summary>
		/// Execute the search on the given instructions
		/// </summary>
		/// <param name="instructions">instructions to search in</param>
		/// <returns>List of all matching sequences, in order</returns>
		/// <exception cref="InvalidDataException">thrown when an unexpected amount of matches is found</exception>
		public List<List<CodeInstruction>> DoSearch(List<CodeInstruction> instructions) {
			int searchSequenceLength = searchMasks.Count;
			List<int> sequenceMatches = InstructionUtils.FindInstructionSequence(instructions, searchMasks.Select(mask => mask.mask).ToList());
			int sequenceMatchesCount = sequenceMatches.Count;

			if (expectedMatches >= 0 && sequenceMatchesCount != expectedMatches) {
				throw new InvalidDataException(
						$"InstructionSearcher has found {sequenceMatchesCount} matches, but expected to find {expectedMatches}! Mod may be outdated!");
			}

			List<List<CodeInstruction>> result = new List<List<CodeInstruction>>();
			foreach (int sequenceMatch in sequenceMatches) {
				List<CodeInstruction> matchedInstructions = new List<CodeInstruction>();
				for (int indexOffset = 0; indexOffset < searchSequenceLength; indexOffset++) {
					if (searchMasks[indexOffset].copyToResult) {
						matchedInstructions.Add(instructions[sequenceMatch + indexOffset]);
					}
				}
				result.Add(matchedInstructions);
			}
			return result;
		}

		/// <summary>
		/// Execute the search on the given instructions
		/// Catches all exceptions and writes them to the logger
		/// </summary>
		/// <param name="instructions">instructions to search in</param>
		/// <param name="logger">logger to write exceptions to</param>
		/// <returns>List of all matching sequences, in order</returns>
		public List<List<CodeInstruction>> DoSearchSafe(List<CodeInstruction> instructions, ManualLogSource logger) {
			try {
				return DoSearch(instructions);
			} catch (InvalidDataException e) {
				string calleeName = new StackFrame(1).GetMethod().Name;
				logger.LogError($"InstructionSearch for method {calleeName} caused error: {e.Message}\n{e.StackTrace}");
				return null;
			}
		}

	}

}
