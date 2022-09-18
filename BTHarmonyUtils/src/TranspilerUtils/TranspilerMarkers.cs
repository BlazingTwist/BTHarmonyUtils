using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace BTHarmonyUtils.TranspilerUtils {

	public static class TranspilerMarkers {

		internal enum Markers {

			insertSequence,
			prefixSequence,
			targetSequence,
			postfixSequence

		}

		public static readonly CodeInstruction ci_insertSequenceStart =
				new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TranspilerMarkers), nameof(InsertSequenceStart)));

		public static readonly CodeInstruction ci_prefixSequenceStart =
				new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TranspilerMarkers), nameof(PrefixSequenceStart)));

		public static readonly CodeInstruction ci_targetSequenceStart =
				new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TranspilerMarkers), nameof(TargetSequenceStart)));

		public static readonly CodeInstruction ci_postfixSequenceStart =
				new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TranspilerMarkers), nameof(PostfixSequenceStart)));

		public static readonly CodeInstruction ci_LastSequenceEnd =
				new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TranspilerMarkers), nameof(LastSequenceEnd)));

		/// <summary>
		/// Marks the start of the InsertSequence in a Marker-Method
		/// </summary>
		[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		public static void InsertSequenceStart() { }

		/// <summary>
		/// Marks the start of the PrefixSequence in a Marker-Method
		/// </summary>
		[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		public static void PrefixSequenceStart() { }

		/// <summary>
		/// Marks the start of the TargetSequence in a Marker-Method
		/// </summary>
		[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		public static void TargetSequenceStart() { }

		/// <summary>
		/// Marks the start of the PostfixSequence in a Marker-Method
		/// </summary>
		[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		public static void PostfixSequenceStart() { }

		/// <summary>
		/// Marks the end of the current Sequence in a Marker-Method
		/// </summary>
		[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		public static void LastSequenceEnd() { }

	}

}
