using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;
using BTHarmonyUtils.@internal;
using HarmonyLib;
using JetBrains.Annotations;

namespace BTHarmonyUtils.ILUtils {

	/// <summary>
	/// A class that houses all the logic for Simplifying Code-Instructions
	/// </summary>
	[PublicAPI]
	public static class InstructionSimplifier {

		/// <summary>
		/// Creates a Tuple&lt;opcode, operand&gt; contains a simplified OpCode/Operand
		/// e.g. 'Ldarg_0' becomes {Ldarg_S, 0}
		/// </summary>
		/// <param name="codeInstruction">codeInstruction to simplify</param>
		/// <returns>the Tuple&lt;opcode, operand&gt;</returns>
		public static Tuple<OpCode?, object> SimplifyForComparison(CodeInstruction codeInstruction) {
			return SimplifyForComparison(codeInstruction.opcode, codeInstruction.operand);
		}

		/// <summary>
		/// Creates a Tuple&lt;opcode, operand&gt; contains a simplified OpCode/Operand
		/// e.g. 'Ldarg_0' becomes {Ldarg_S, 0}
		/// </summary>
		/// <param name="opcode">the opCode to simplify</param>
		/// <param name="operand">the operand to simplify</param>
		/// <returns>the Tuple&lt;opcode, operand&gt;</returns>
		[SuppressMessage("ReSharper", "ConvertIfStatementToReturnStatement")]
		public static Tuple<OpCode?, object> SimplifyForComparison(OpCode opcode, object operand) {
			if (opcode == OpCodes.Ldarg_0) {
				return new Tuple<OpCode?, object>(OpCodes.Ldarg_S, 0);
			}
			if (opcode == OpCodes.Ldarg_1) {
				return new Tuple<OpCode?, object>(OpCodes.Ldarg_S, 1);
			}
			if (opcode == OpCodes.Ldarg_2) {
				return new Tuple<OpCode?, object>(OpCodes.Ldarg_S, 2);
			}
			if (opcode == OpCodes.Ldarg_3) {
				return new Tuple<OpCode?, object>(OpCodes.Ldarg_S, 3);
			}
			if (opcode == OpCodes.Ldarg) {
				return new Tuple<OpCode?, object>(OpCodes.Ldarg_S, operand);
			}

			if (opcode == OpCodes.Ldloc_0) {
				return new Tuple<OpCode?, object>(OpCodes.Ldloc_S, 0);
			}
			if (opcode == OpCodes.Ldloc_1) {
				return new Tuple<OpCode?, object>(OpCodes.Ldloc_S, 1);
			}
			if (opcode == OpCodes.Ldloc_2) {
				return new Tuple<OpCode?, object>(OpCodes.Ldloc_S, 2);
			}
			if (opcode == OpCodes.Ldloc_3) {
				return new Tuple<OpCode?, object>(OpCodes.Ldloc_S, 3);
			}
			if (opcode == OpCodes.Ldloc) {
				return new Tuple<OpCode?, object>(OpCodes.Ldloc_S, operand);
			}

			if (opcode == OpCodes.Stloc_0) {
				return new Tuple<OpCode?, object>(OpCodes.Stloc_S, 0);
			}
			if (opcode == OpCodes.Stloc_1) {
				return new Tuple<OpCode?, object>(OpCodes.Stloc_S, 1);
			}
			if (opcode == OpCodes.Stloc_2) {
				return new Tuple<OpCode?, object>(OpCodes.Stloc_S, 2);
			}
			if (opcode == OpCodes.Stloc_3) {
				return new Tuple<OpCode?, object>(OpCodes.Stloc_S, 3);
			}
			if (opcode == OpCodes.Stloc) {
				return new Tuple<OpCode?, object>(OpCodes.Stloc_S, operand);
			}

			if (opcode == OpCodes.Ldc_I4_0) {
				return new Tuple<OpCode?, object>(OpCodes.Ldc_I4_S, 0);
			}
			if (opcode == OpCodes.Ldc_I4_1) {
				return new Tuple<OpCode?, object>(OpCodes.Ldc_I4_S, 1);
			}
			if (opcode == OpCodes.Ldc_I4_2) {
				return new Tuple<OpCode?, object>(OpCodes.Ldc_I4_S, 2);
			}
			if (opcode == OpCodes.Ldc_I4_3) {
				return new Tuple<OpCode?, object>(OpCodes.Ldc_I4_S, 3);
			}
			if (opcode == OpCodes.Ldc_I4_4) {
				return new Tuple<OpCode?, object>(OpCodes.Ldc_I4_S, 4);
			}
			if (opcode == OpCodes.Ldc_I4_5) {
				return new Tuple<OpCode?, object>(OpCodes.Ldc_I4_S, 5);
			}
			if (opcode == OpCodes.Ldc_I4_6) {
				return new Tuple<OpCode?, object>(OpCodes.Ldc_I4_S, 6);
			}
			if (opcode == OpCodes.Ldc_I4_7) {
				return new Tuple<OpCode?, object>(OpCodes.Ldc_I4_S, 7);
			}
			if (opcode == OpCodes.Ldc_I4_8) {
				return new Tuple<OpCode?, object>(OpCodes.Ldc_I4_S, 8);
			}
			if (opcode == OpCodes.Ldc_I4_M1) {
				return new Tuple<OpCode?, object>(OpCodes.Ldc_I4_S, -1);
			}
			if (opcode == OpCodes.Ldc_I4) {
				return new Tuple<OpCode?, object>(OpCodes.Ldc_I4_S, operand);
			}

			if (opcode == OpCodes.Leave) {
				return new Tuple<OpCode?, object>(OpCodes.Leave_S, operand);
			}

			if (opcode == OpCodes.Br) {
				return new Tuple<OpCode?, object>(OpCodes.Br_S, operand);
			}

			if (opcode == OpCodes.Brfalse) {
				return new Tuple<OpCode?, object>(OpCodes.Brfalse_S, operand);
			}

			if (opcode == OpCodes.Brtrue) {
				return new Tuple<OpCode?, object>(OpCodes.Brtrue_S, operand);
			}

			if (opcode == OpCodes.Beq) {
				return new Tuple<OpCode?, object>(OpCodes.Beq_S, operand);
			}

			if (opcode == OpCodes.Bne_Un) {
				return new Tuple<OpCode?, object>(OpCodes.Bne_Un_S, operand);
			}

			if (opcode == OpCodes.Bge || opcode == OpCodes.Bge_Un || opcode == OpCodes.Bge_Un_S) {
				return new Tuple<OpCode?, object>(OpCodes.Bge_S, operand);
			}

			if (opcode == OpCodes.Ble || opcode == OpCodes.Ble_Un || opcode == OpCodes.Ble_Un_S) {
				return new Tuple<OpCode?, object>(OpCodes.Ble_S, operand);
			}

			if (opcode == OpCodes.Bgt || opcode == OpCodes.Bgt_Un || opcode == OpCodes.Bgt_Un_S) {
				return new Tuple<OpCode?, object>(OpCodes.Bgt_S, operand);
			}

			if (opcode == OpCodes.Blt || opcode == OpCodes.Blt_Un || opcode == OpCodes.Blt_Un_S) {
				return new Tuple<OpCode?, object>(OpCodes.Blt_S, operand);
			}

			if (opcode == OpCodes.Callvirt || opcode == OpCodes.Calli) {
				return new Tuple<OpCode?, object>(OpCodes.Call, operand);
			}

			return new Tuple<OpCode?, object>(opcode, operand);
		}

	}

}
