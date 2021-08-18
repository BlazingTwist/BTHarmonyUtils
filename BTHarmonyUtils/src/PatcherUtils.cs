using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace BTHarmonyUtils {
	public static class PatcherUtils {
		/// <summary>
		/// This makes patching IEnumerator Methods that use `yield` easier.
		/// Searches a method returning `IEnumerator` for the actual implementation of `MoveNext`
		/// 
		/// Beware when Pre- or Post-fixing: the MoveNext method will be called once for every value that the IEnumerator returns!
		/// If you want your Postfix to execute after the last value is returned, you can check if the `bool __result` value is `false`
		///   (indicates that all elements have been iterated over)
		/// </summary>
		/// <param name="method">The Method returning `IEnumerator`</param>
		/// <returns>The MoveNext implementation for the IEnumerator</returns>
		public static MethodInfo FindIEnumeratorMoveNext(MethodBase method) {
			if (method is null) {
				throw new ArgumentNullException(nameof(method));
			}
			
			foreach (CodeInstruction instruction in new MethodBodyReader(method).ReadInstructions()) {
				if (instruction.opcode == OpCodes.Newobj && instruction.operand is ConstructorInfo ctor) {
					return AccessTools.Method(ctor.DeclaringType, nameof(IEnumerator.MoveNext));
				}
			}
			return null;
		}
	}
}