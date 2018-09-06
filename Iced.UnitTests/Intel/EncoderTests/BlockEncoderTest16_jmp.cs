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
using Iced.Intel;
using Xunit;

namespace Iced.UnitTests.Intel.EncoderTests {
	public sealed class BlockEncoderTest16_jmp : BlockEncoderTest {
		const int bitness = 16;
		const ulong origRip = 0x8000;
		const ulong newRip = 0xF000;

		[Fact]
		void Jmp_fwd() {
			var originalData = new byte[] {
				/*0000*/ 0xB0, 0x00,// mov al,0
				/*0002*/ 0xEB, 0x07,// jmp short 800Bh
				/*0004*/ 0xB0, 0x01,// mov al,1
				/*0006*/ 0xE9, 0x02, 0x00,// jmp near ptr 800Bh
				/*0009*/ 0xB0, 0x02,// mov al,2
				/*000B*/ 0x90,// nop
			};
			var newData = new byte[] {
				/*0000*/ 0xB0, 0x00,// mov al,0
				/*0002*/ 0xEB, 0x06,// jmp short 0F00Ah
				/*0004*/ 0xB0, 0x01,// mov al,1
				/*0006*/ 0xEB, 0x02,// jmp short 0F00Ah
				/*0008*/ 0xB0, 0x02,// mov al,2
				/*000A*/ 0x90,// nop
			};
			var expectedInstructionOffsets = new uint[] {
				0x0000,
				0x0002,
				0x0004,
				0x0006,
				0x0008,
				0x000A,
			};
			var expectedRelocInfos = Array.Empty<RelocInfo>();
			const BlockEncoderOptions options = BlockEncoderOptions.None;
			EncodeBase(bitness, origRip, originalData, newRip, newData, options, expectedInstructionOffsets, expectedRelocInfos);
		}

		[Fact]
		void Jmp_bwd() {
			var originalData = new byte[] {
				/*0000*/ 0x90,// nop
				/*0001*/ 0xB0, 0x00,// mov al,0
				/*0003*/ 0xEB, 0xFB,// jmp short 8000h
				/*0005*/ 0xB0, 0x01,// mov al,1
				/*0007*/ 0xE9, 0xF6, 0xFF,// jmp near ptr 8000h
				/*000A*/ 0xB0, 0x02,// mov al,2
			};
			var newData = new byte[] {
				/*0000*/ 0x90,// nop
				/*0001*/ 0xB0, 0x00,// mov al,0
				/*0003*/ 0xEB, 0xFB,// jmp short 0F000h
				/*0005*/ 0xB0, 0x01,// mov al,1
				/*0007*/ 0xEB, 0xF7,// jmp short 0F000h
				/*0009*/ 0xB0, 0x02,// mov al,2
			};
			var expectedInstructionOffsets = new uint[] {
				0x0000,
				0x0001,
				0x0003,
				0x0005,
				0x0007,
				0x0009,
			};
			var expectedRelocInfos = Array.Empty<RelocInfo>();
			const BlockEncoderOptions options = BlockEncoderOptions.None;
			EncodeBase(bitness, origRip, originalData, newRip, newData, options, expectedInstructionOffsets, expectedRelocInfos);
		}

		[Fact]
		void Jmp_other_short_os() {
			var originalData = new byte[] {
				/*0000*/ 0xB0, 0x00,// mov al,0
				/*0002*/ 0x66, 0xEB, 0x0A,// jmp short 0000800Fh
				/*0005*/ 0xB0, 0x01,// mov al,1
				/*0007*/ 0x66, 0xE9, 0x02, 0x00, 0x00, 0x00,// jmp near ptr 0000800Fh
				/*000D*/ 0xB0, 0x02,// mov al,2
			};
			var newData = new byte[] {
				/*0000*/ 0xB0, 0x00,// mov al,0
				/*0002*/ 0x66, 0xEB, 0x0B,// jmp short 0000800Fh
				/*0005*/ 0xB0, 0x01,// mov al,1
				/*0007*/ 0x66, 0xEB, 0x06,// jmp short 0000800Fh
				/*000A*/ 0xB0, 0x02,// mov al,2
			};
			var expectedInstructionOffsets = new uint[] {
				0x0000,
				0x0002,
				0x0005,
				0x0007,
				0x000A,
			};
			var expectedRelocInfos = Array.Empty<RelocInfo>();
			const BlockEncoderOptions options = BlockEncoderOptions.None;
			EncodeBase(bitness, origRip, originalData, origRip - 1, newData, options, expectedInstructionOffsets, expectedRelocInfos);
		}

		[Fact]
		void Jmp_other_near_os() {
			var originalData = new byte[] {
				/*0000*/ 0xB0, 0x00,// mov al,0
				/*0002*/ 0x66, 0xEB, 0x0A,// jmp short 0000800Fh
				/*0005*/ 0xB0, 0x01,// mov al,1
				/*0007*/ 0x66, 0xE9, 0x02, 0x00, 0x00, 0x00,// jmp near ptr 0000800Fh
				/*000D*/ 0xB0, 0x02,// mov al,2
			};
			var newData = new byte[] {
				/*0000*/ 0xB0, 0x00,// mov al,0
				/*0002*/ 0x66, 0xE9, 0x07, 0xF0, 0xFF, 0xFF,// jmp near ptr 0000800Fh
				/*0008*/ 0xB0, 0x01,// mov al,1
				/*000A*/ 0x66, 0xE9, 0xFF, 0xEF, 0xFF, 0xFF,// jmp near ptr 0000800Fh
				/*0010*/ 0xB0, 0x02,// mov al,2
			};
			var expectedInstructionOffsets = new uint[] {
				0x0000,
				0x0002,
				0x0008,
				0x000A,
				0x0010,
			};
			var expectedRelocInfos = Array.Empty<RelocInfo>();
			const BlockEncoderOptions options = BlockEncoderOptions.None;
			EncodeBase(bitness, origRip, originalData, origRip + 0x1000, newData, options, expectedInstructionOffsets, expectedRelocInfos);
		}

		[Fact]
		void Jmp_other_short() {
			var originalData = new byte[] {
				/*0000*/ 0xB0, 0x00,// mov al,0
				/*0002*/ 0xEB, 0x07,// jmp short 800Bh
				/*0004*/ 0xB0, 0x01,// mov al,1
				/*0006*/ 0xE9, 0x02, 0x00,// jmp near ptr 800Bh
				/*0009*/ 0xB0, 0x02,// mov al,2
			};
			var newData = new byte[] {
				/*0000*/ 0xB0, 0x00,// mov al,0
				/*0002*/ 0xEB, 0x08,// jmp short 800Bh
				/*0004*/ 0xB0, 0x01,// mov al,1
				/*0006*/ 0xEB, 0x04,// jmp short 800Bh
				/*0008*/ 0xB0, 0x02,// mov al,2
			};
			var expectedInstructionOffsets = new uint[] {
				0x0000,
				0x0002,
				0x0004,
				0x0006,
				0x0008,
			};
			var expectedRelocInfos = Array.Empty<RelocInfo>();
			const BlockEncoderOptions options = BlockEncoderOptions.None;
			EncodeBase(bitness, origRip, originalData, origRip - 1, newData, options, expectedInstructionOffsets, expectedRelocInfos);
		}

		[Fact]
		void Jmp_other_near() {
			var originalData = new byte[] {
				/*0000*/ 0xB0, 0x00,// mov al,0
				/*0002*/ 0xEB, 0x07,// jmp short 800Bh
				/*0004*/ 0xB0, 0x01,// mov al,1
				/*0006*/ 0xE9, 0x02, 0x00,// jmp near ptr 800Bh
				/*0009*/ 0xB0, 0x02,// mov al,2
			};
			var newData = new byte[] {
				/*0000*/ 0xB0, 0x00,// mov al,0
				/*0002*/ 0xE9, 0x06, 0xF0,// jmp near ptr 800Bh
				/*0005*/ 0xB0, 0x01,// mov al,1
				/*0007*/ 0xE9, 0x01, 0xF0,// jmp near ptr 800Bh
				/*000A*/ 0xB0, 0x02,// mov al,2
			};
			var expectedInstructionOffsets = new uint[] {
				0x0000,
				0x0002,
				0x0005,
				0x0007,
				0x000A,
			};
			var expectedRelocInfos = Array.Empty<RelocInfo>();
			const BlockEncoderOptions options = BlockEncoderOptions.None;
			EncodeBase(bitness, origRip, originalData, origRip + 0x1000, newData, options, expectedInstructionOffsets, expectedRelocInfos);
		}

		[Fact]
		void Jmp_fwd_no_opt() {
			var originalData = new byte[] {
				/*0000*/ 0xB0, 0x00,// mov al,0
				/*0002*/ 0xEB, 0x07,// jmp short 800Bh
				/*0004*/ 0xB0, 0x01,// mov al,1
				/*0006*/ 0xE9, 0x02, 0x00,// jmp near ptr 800Bh
				/*0009*/ 0xB0, 0x02,// mov al,2
				/*000B*/ 0x90,// nop
			};
			var newData = new byte[] {
				/*0000*/ 0xB0, 0x00,// mov al,0
				/*0002*/ 0xEB, 0x07,// jmp short 800Bh
				/*0004*/ 0xB0, 0x01,// mov al,1
				/*0006*/ 0xE9, 0x02, 0x00,// jmp near ptr 800Bh
				/*0009*/ 0xB0, 0x02,// mov al,2
				/*000B*/ 0x90,// nop
			};
			var expectedInstructionOffsets = new uint[] {
				0x0000,
				0x0002,
				0x0004,
				0x0006,
				0x0009,
				0x000B,
			};
			var expectedRelocInfos = Array.Empty<RelocInfo>();
			const BlockEncoderOptions options = BlockEncoderOptions.DontFixBranches;
			EncodeBase(bitness, origRip, originalData, newRip, newData, options, expectedInstructionOffsets, expectedRelocInfos);
		}
	}
}
#endif