﻿/*
    Copyright (C) 2018 de4dot@gmail.com

    This file is part of Iced.

    Iced is free software: you can redistribute it and/or modify
    it under the terms of the GNU Lesser General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Iced is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public License
    along with Iced.  If not, see <https://www.gnu.org/licenses/>.
*/

#if !NO_ENCODER
using System;
using System.Diagnostics;

namespace Iced.Intel.BlockEncoderInternal {
	/// <summary>
	/// Simple branch instruction that only has one code value, eg. loopcc, jrcxz
	/// </summary>
	sealed class SimpleBranchInstr : Instr {
		readonly int bitness;
		Instruction instruction;
		TargetInstr targetInstr;
		BlockData pointerData;
		InstrKind instrKind;
		readonly uint shortInstructionSize;
		readonly uint nearInstructionSize;
		readonly uint longInstructionSize;
		readonly uint nativeInstructionSize;
		readonly Code nativeCode;

		enum InstrKind {
			Unchanged,
			Short,
			Near,
			Long,
			Uninitialized,
		}

		public SimpleBranchInstr(BlockEncoder blockEncoder, ref Instruction instruction)
			: base(blockEncoder, instruction.IP64) {
			bitness = blockEncoder.Bitness;
			this.instruction = instruction;
			instrKind = InstrKind.Uninitialized;

			string errorMessage;

			if (!blockEncoder.FixBranches) {
				instrKind = InstrKind.Unchanged;
				Size = (uint)blockEncoder.NullEncoder.Encode(ref instruction, instruction.IP64, out errorMessage);
				if (errorMessage != null)
					Size = DecoderConstants.MaxInstructionLength;
			}
			else {
				Instruction instrCopy;

				instrCopy = instruction;
				instrCopy.NearBranch64Target = 0;
				shortInstructionSize = (uint)blockEncoder.NullEncoder.Encode(ref instrCopy, 0, out errorMessage);
				if (errorMessage != null)
					shortInstructionSize = DecoderConstants.MaxInstructionLength;

				nativeCode = ToNativeBranchCode(instruction.Code, blockEncoder.Bitness);
				if (nativeCode == instruction.Code)
					nativeInstructionSize = shortInstructionSize;
				else {
					instrCopy = instruction;
					instrCopy.Code = nativeCode;
					instrCopy.NearBranch64Target = 0;
					nativeInstructionSize = (uint)blockEncoder.NullEncoder.Encode(ref instrCopy, 0, out errorMessage);
					if (errorMessage != null)
						nativeInstructionSize = DecoderConstants.MaxInstructionLength;
				}

				switch (blockEncoder.Bitness) {
				case 16:
					nearInstructionSize = nativeInstructionSize + 2 + 3;
					break;

				case 32:
				case 64:
					nearInstructionSize = nativeInstructionSize + 2 + 5;
					break;

				default:
					throw new InvalidOperationException();
				}

				if (blockEncoder.Bitness == 64) {
					longInstructionSize = nativeInstructionSize + 2 + CallOrJmpPointerDataInstructionSize64;
					Size = Math.Max(Math.Max(shortInstructionSize, nearInstructionSize), longInstructionSize);
				}
				else
					Size = Math.Max(shortInstructionSize, nearInstructionSize);
			}
		}

		static Code ToNativeBranchCode(Code code, int bitness) {
			Code c16, c32, c64;
			switch (code) {
			case Code.Loopne_Jb16_CX:
			case Code.Loopne_Jb32_CX:
				c16 = Code.Loopne_Jb16_CX;
				c32 = Code.Loopne_Jb32_CX;
				c64 = Code.INVALID;
				break;

			case Code.Loopne_Jb16_ECX:
			case Code.Loopne_Jb32_ECX:
			case Code.Loopne_Jb64_ECX:
				c16 = Code.Loopne_Jb16_ECX;
				c32 = Code.Loopne_Jb32_ECX;
				c64 = Code.Loopne_Jb64_ECX;
				break;

			case Code.Loopne_Jb64_RCX:
				c16 = Code.INVALID;
				c32 = Code.INVALID;
				c64 = Code.Loopne_Jb64_RCX;
				break;

			case Code.Loope_Jb16_CX:
			case Code.Loope_Jb32_CX:
				c16 = Code.Loope_Jb16_CX;
				c32 = Code.Loope_Jb32_CX;
				c64 = Code.INVALID;
				break;

			case Code.Loope_Jb16_ECX:
			case Code.Loope_Jb32_ECX:
			case Code.Loope_Jb64_ECX:
				c16 = Code.Loope_Jb16_ECX;
				c32 = Code.Loope_Jb32_ECX;
				c64 = Code.Loope_Jb64_ECX;
				break;

			case Code.Loope_Jb64_RCX:
				c16 = Code.INVALID;
				c32 = Code.INVALID;
				c64 = Code.Loope_Jb64_RCX;
				break;

			case Code.Loop_Jb16_CX:
			case Code.Loop_Jb32_CX:
				c16 = Code.Loop_Jb16_CX;
				c32 = Code.Loop_Jb32_CX;
				c64 = Code.INVALID;
				break;

			case Code.Loop_Jb16_ECX:
			case Code.Loop_Jb32_ECX:
			case Code.Loop_Jb64_ECX:
				c16 = Code.Loop_Jb16_ECX;
				c32 = Code.Loop_Jb32_ECX;
				c64 = Code.Loop_Jb64_ECX;
				break;

			case Code.Loop_Jb64_RCX:
				c16 = Code.INVALID;
				c32 = Code.INVALID;
				c64 = Code.Loop_Jb64_RCX;
				break;

			case Code.Jcxz_Jb16:
			case Code.Jcxz_Jb32:
				c16 = Code.Jcxz_Jb16;
				c32 = Code.Jcxz_Jb32;
				c64 = Code.INVALID;
				break;

			case Code.Jecxz_Jb16:
			case Code.Jecxz_Jb32:
			case Code.Jecxz_Jb64:
				c16 = Code.Jecxz_Jb16;
				c32 = Code.Jecxz_Jb32;
				c64 = Code.Jecxz_Jb64;
				break;

			case Code.Jrcxz_Jb64:
				c16 = Code.INVALID;
				c32 = Code.INVALID;
				c64 = Code.Jrcxz_Jb64;
				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(code));
			}

			switch (bitness) {
			case 16: return c16;
			case 32: return c32;
			case 64: return c64;
			default: throw new ArgumentOutOfRangeException(nameof(bitness));
			}
		}

		public override void Initialize() {
			targetInstr = blockEncoder.GetTarget(instruction.NearBranchTarget);
			TryOptimize();
		}

		public override bool Optimize() => TryOptimize();

		bool TryOptimize() {
			if (instrKind == InstrKind.Unchanged || instrKind == InstrKind.Short)
				return false;

			var targetAddress = targetInstr.GetAddress();
			var nextRip = IP + shortInstructionSize;
			long diff = (long)(targetAddress - nextRip);
			if (sbyte.MinValue <= diff && diff <= sbyte.MaxValue) {
				if (pointerData != null)
					pointerData.IsValid = false;
				instrKind = InstrKind.Short;
				Size = shortInstructionSize;
				return true;
			}

			// If it's in the same block, we assume the target is at most 2GB away.
			bool useNear = bitness != 64 || targetInstr.IsInBlock(Block);
			if (!useNear) {
				targetAddress = targetInstr.GetAddress();
				nextRip = IP + nearInstructionSize;
				diff = (long)(targetAddress - nextRip);
				useNear = int.MinValue <= diff && diff <= int.MaxValue;
			}
			if (useNear) {
				if (pointerData != null)
					pointerData.IsValid = false;
				instrKind = InstrKind.Near;
				Size = nearInstructionSize;
				return true;
			}

			if (pointerData == null)
				pointerData = Block.AllocPointerLocation();
			instrKind = InstrKind.Long;
			return false;
		}

		public override string TryEncode(Encoder encoder, out ConstantOffsets constantOffsets, out bool isOriginalInstruction) {
			string errorMessage;
			Instruction instr;
			uint size;
			switch (instrKind) {
			case InstrKind.Unchanged:
			case InstrKind.Short:
				isOriginalInstruction = true;
				instruction.NearBranch64Target = targetInstr.GetAddress();
				encoder.Encode(ref instruction, IP, out errorMessage);
				if (errorMessage != null) {
					constantOffsets = default;
					return CreateErrorMessage(errorMessage, ref instruction);
				}
				constantOffsets = encoder.GetConstantOffsets();
				return null;

			case InstrKind.Near:
				isOriginalInstruction = false;
				constantOffsets = default;

				// Code:
				//		brins tmp		; nativeInstructionSize
				//		jmp short skip	; 2
				//	tmp:
				//		jmp near target	; 3/5/5
				//	skip:

				instr = instruction;
				instr.Code = nativeCode;
				instr.NearBranch64Target = IP + nativeInstructionSize + 2;
				size = (uint)encoder.Encode(ref instr, IP, out errorMessage);
				if (errorMessage != null)
					return CreateErrorMessage(errorMessage, ref instruction);

				instr = new Instruction();
				instr.OpCount = 1;
				instr.NearBranch64Target = IP + nearInstructionSize;
				Code codeNear;
				switch (encoder.Bitness) {
				case 16:
					instr.Code = Code.Jmp_Jb16;
					codeNear = Code.Jmp_Jw16;
					instr.Op0Kind = OpKind.NearBranch16;
					break;

				case 32:
					instr.Code = Code.Jmp_Jb32;
					codeNear = Code.Jmp_Jd32;
					instr.Op0Kind = OpKind.NearBranch32;
					break;

				case 64:
					instr.Code = Code.Jmp_Jb64;
					codeNear = Code.Jmp_Jd64;
					instr.Op0Kind = OpKind.NearBranch64;
					break;

				default:
					throw new InvalidOperationException();
				}
				size += (uint)encoder.Encode(ref instr, IP + size, out errorMessage);
				if (errorMessage != null)
					return CreateErrorMessage(errorMessage, ref instruction);

				instr.Code = codeNear;
				instr.NearBranch64Target = targetInstr.GetAddress();
				encoder.Encode(ref instr, IP + size, out errorMessage);
				if (errorMessage != null)
					return CreateErrorMessage(errorMessage, ref instruction);
				return null;

			case InstrKind.Long:
				Debug.Assert(encoder.Bitness == 64);
				Debug.Assert(pointerData != null);
				isOriginalInstruction = false;
				constantOffsets = default;
				pointerData.Data = targetInstr.GetAddress();

				// Code:
				//		brins tmp		; nativeInstructionSize
				//		jmp short skip	; 2
				//	tmp:
				//		jmp [mem_loc]	; 6
				//	skip:

				instr = instruction;
				instr.Code = nativeCode;
				instr.NearBranch64Target = IP + nativeInstructionSize + 2;
				size = (uint)encoder.Encode(ref instr, IP, out errorMessage);
				if (errorMessage != null)
					return CreateErrorMessage(errorMessage, ref instruction);

				instr = new Instruction();
				instr.OpCount = 1;
				instr.NearBranch64Target = IP + longInstructionSize;
				switch (encoder.Bitness) {
				case 16:
					instr.Code = Code.Jmp_Jb16;
					instr.Op0Kind = OpKind.NearBranch16;
					break;

				case 32:
					instr.Code = Code.Jmp_Jb32;
					instr.Op0Kind = OpKind.NearBranch32;
					break;

				case 64:
					instr.Code = Code.Jmp_Jb64;
					instr.Op0Kind = OpKind.NearBranch64;
					break;

				default:
					throw new InvalidOperationException();
				}
				size += (uint)encoder.Encode(ref instr, IP + size, out errorMessage);
				if (errorMessage != null)
					return CreateErrorMessage(errorMessage, ref instruction);

				errorMessage = EncodeBranchToPointerData(encoder, isCall: false, IP + size, pointerData, out _, Size - size);
				if (errorMessage != null)
					return CreateErrorMessage(errorMessage, ref instruction);
				return null;

			case InstrKind.Uninitialized:
			default:
				throw new InvalidOperationException();
			}
		}
	}
}
#endif