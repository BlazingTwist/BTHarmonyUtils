using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;
using HarmonyLib;

namespace BTHarmonyUtils {
	public static class InstructionSimplifier {
		[SuppressMessage("ReSharper", "ConvertIfStatementToReturnStatement")]
		public static Tuple<OpCode, object> SimplifyForComparison(CodeInstruction instruction) {
			OpCode opcode = instruction.opcode;
			
			if (opcode == OpCodes.Ldarg_0) {
				return new Tuple<OpCode, object>(OpCodes.Ldarg_S, 0);
			}
			if (opcode == OpCodes.Ldarg_1) {
				return new Tuple<OpCode, object>(OpCodes.Ldarg_S, 1);
			}
			if (opcode == OpCodes.Ldarg_2) {
				return new Tuple<OpCode, object>(OpCodes.Ldarg_S, 2);
			}
			if (opcode == OpCodes.Ldarg_3) {
				return new Tuple<OpCode, object>(OpCodes.Ldarg_S, 3);
			}
			if (opcode == OpCodes.Ldarg) {
				return new Tuple<OpCode, object>(OpCodes.Ldarg_S, instruction.operand);
			}

			if (opcode == OpCodes.Ldloc_0) {
				return new Tuple<OpCode, object>(OpCodes.Ldloc_S, 0);
			}
			if (opcode == OpCodes.Ldloc_1) {
				return new Tuple<OpCode, object>(OpCodes.Ldloc_S, 1);
			}
			if (opcode == OpCodes.Ldloc_2) {
				return new Tuple<OpCode, object>(OpCodes.Ldloc_S, 2);
			}
			if (opcode == OpCodes.Ldloc_3) {
				return new Tuple<OpCode, object>(OpCodes.Ldloc_S, 3);
			}
			if (opcode == OpCodes.Ldloc) {
				return new Tuple<OpCode, object>(OpCodes.Ldloc_S, instruction.operand);
			}

			if (opcode == OpCodes.Stloc_0) {
				return new Tuple<OpCode, object>(OpCodes.Stloc_S, 0);
			}
			if (opcode == OpCodes.Stloc_1) {
				return new Tuple<OpCode, object>(OpCodes.Stloc_S, 1);
			}
			if (opcode == OpCodes.Stloc_2) {
				return new Tuple<OpCode, object>(OpCodes.Stloc_S, 2);
			}
			if (opcode == OpCodes.Stloc_3) {
				return new Tuple<OpCode, object>(OpCodes.Stloc_S, 3);
			}
			if (opcode == OpCodes.Stloc) {
				return new Tuple<OpCode, object>(OpCodes.Stloc_S, instruction.operand);
			}

			if (opcode == OpCodes.Ldc_I4_0) {
				return new Tuple<OpCode, object>(OpCodes.Ldc_I4_S, 0);
			}
			if (opcode == OpCodes.Ldc_I4_1) {
				return new Tuple<OpCode, object>(OpCodes.Ldc_I4_S, 1);
			}
			if (opcode == OpCodes.Ldc_I4_2) {
				return new Tuple<OpCode, object>(OpCodes.Ldc_I4_S, 2);
			}
			if (opcode == OpCodes.Ldc_I4_3) {
				return new Tuple<OpCode, object>(OpCodes.Ldc_I4_S, 3);
			}
			if (opcode == OpCodes.Ldc_I4_4) {
				return new Tuple<OpCode, object>(OpCodes.Ldc_I4_S, 4);
			}
			if (opcode == OpCodes.Ldc_I4_5) {
				return new Tuple<OpCode, object>(OpCodes.Ldc_I4_S, 5);
			}
			if (opcode == OpCodes.Ldc_I4_6) {
				return new Tuple<OpCode, object>(OpCodes.Ldc_I4_S, 6);
			}
			if (opcode == OpCodes.Ldc_I4_7) {
				return new Tuple<OpCode, object>(OpCodes.Ldc_I4_S, 7);
			}
			if (opcode == OpCodes.Ldc_I4_8) {
				return new Tuple<OpCode, object>(OpCodes.Ldc_I4_S, 8);
			}
			if (opcode == OpCodes.Ldc_I4_M1) {
				return new Tuple<OpCode, object>(OpCodes.Ldc_I4_S, -1);
			}
			if (opcode == OpCodes.Ldc_I4) {
				return new Tuple<OpCode, object>(OpCodes.Ldc_I4_S, instruction.operand);
			}

			if (opcode == OpCodes.Leave) {
				return new Tuple<OpCode, object>(OpCodes.Leave_S, instruction.operand);
			}

			if (opcode == OpCodes.Br) {
				return new Tuple<OpCode, object>(OpCodes.Br_S, instruction.operand);
			}

			if (opcode == OpCodes.Brfalse) {
				return new Tuple<OpCode, object>(OpCodes.Brfalse_S, instruction.operand);
			}

			if (opcode == OpCodes.Brtrue) {
				return new Tuple<OpCode, object>(OpCodes.Brtrue_S, instruction.operand);
			}

			if (opcode == OpCodes.Beq) {
				return new Tuple<OpCode, object>(OpCodes.Beq_S, instruction.operand);
			}
			
			if (opcode == OpCodes.Bne_Un) {
				return new Tuple<OpCode, object>(OpCodes.Bne_Un_S, instruction.operand);
			}

			if (opcode == OpCodes.Bge || opcode == OpCodes.Bge_Un || opcode == OpCodes.Bge_Un_S) {
				return new Tuple<OpCode, object>(OpCodes.Bge_S, instruction.operand);
			}

			if (opcode == OpCodes.Ble || opcode == OpCodes.Ble_Un || opcode == OpCodes.Ble_Un_S) {
				return new Tuple<OpCode, object>(OpCodes.Ble_S, instruction.operand);
			}

			if (opcode == OpCodes.Bgt || opcode == OpCodes.Bgt_Un || opcode == OpCodes.Bgt_Un_S) {
				return new Tuple<OpCode, object>(OpCodes.Bgt_S, instruction.operand);
			}

			if (opcode == OpCodes.Blt || opcode == OpCodes.Blt_Un || opcode == OpCodes.Blt_Un_S) {
				return new Tuple<OpCode, object>(OpCodes.Blt_S, instruction.operand);
			}
			
			return new Tuple<OpCode, object>(instruction.opcode, instruction.operand);
		}
	}
}