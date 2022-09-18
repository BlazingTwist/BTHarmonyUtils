using System;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;

namespace BTHarmonyUtils.MidFixPatch {

	/// <summary>
	/// Marks a HarmonyPatch method as a MidFix (called in the middle of a method, rather than the beginning or end)
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
	[PublicAPI]
	public class BTHarmonyMidFix : Attribute {

		public Type instructionMatcherDeclaringType { get; }
		public string instructionMatcherMethodName { get; }
		public int priority { get; }

		/// <param name="instructionMatcherMethodName">name of the method that provides the instruction matcher for this midFix</param>
		/// <param name="priority">if you are using multiple midFix patches for the same original method, you can specify their priority, higher priorities execute first</param>
		public BTHarmonyMidFix(string instructionMatcherMethodName, int priority = 1) {
			this.instructionMatcherMethodName = instructionMatcherMethodName;
			this.priority = priority;
		}

		/// <param name="instructionMatcherDeclaringType">type containing the instruction matcher method - only required if in a different class</param>
		/// <param name="instructionMatcherMethodName">name of the method that provides the instruction matcher for this midFix</param>
		/// <param name="priority">if you are using multiple midFix patches for the same original method, you can specify their priority, higher priorities execute first</param>
		public BTHarmonyMidFix(Type instructionMatcherDeclaringType, string instructionMatcherMethodName, int priority = 1) {
			this.instructionMatcherDeclaringType = instructionMatcherDeclaringType;
			this.instructionMatcherMethodName = instructionMatcherMethodName;
			this.priority = priority;
		}

		/// <summary>
		/// Resolves the method that provides the MidFixInstructionMatcher for this patch
		/// </summary>
		public MethodInfo ResolveInstructionMatcherMethod(MethodInfo annotatedMethod) {
			if (instructionMatcherDeclaringType == null && annotatedMethod == null) {
				throw new ArgumentNullException(nameof(annotatedMethod));
			}
			Type declaringType = instructionMatcherDeclaringType ?? annotatedMethod.DeclaringType;
			if (string.IsNullOrEmpty(instructionMatcherMethodName)) {
				throw new ArgumentNullException(
						nameof(instructionMatcherMethodName),
						$"{annotatedMethod.FullDescription()} is annotated with {nameof(BTHarmonyMidFix)}, but no instructionMatcher was provided");
			}
			return AccessTools.DeclaredMethod(declaringType, instructionMatcherMethodName) ?? AccessTools.Method(declaringType, instructionMatcherMethodName);
		}

	}

}
