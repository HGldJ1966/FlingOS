﻿#region LICENSE

// ---------------------------------- LICENSE ---------------------------------- //
//
//    Fling OS - The educational operating system
//    Copyright (C) 2015 Edward Nutting
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 2 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
//  Project owner: 
//		Email: edwardnutting@outlook.com
//		For paper mail address, please contact via email for details.
//
// ------------------------------------------------------------------------------ //

#endregion

using System;
using System.Linq;
using Drivers.Compiler.Architectures.x86.ASMOps;
using Drivers.Compiler.IL;
using Drivers.Compiler.Types;

namespace Drivers.Compiler.Architectures.x86
{
    /// <summary>
    ///     See base class documentation.
    /// </summary>
    public class Ldloc : IL.ILOps.Ldloc
    {
        public override void PerformStackOperations(ILPreprocessState conversionState, ILOp theOp)
        {
            bool loadAddr = (OpCodes) theOp.opCode.Value == OpCodes.Ldloca ||
                            (OpCodes) theOp.opCode.Value == OpCodes.Ldloca_S;
            ushort localIndex = 0;
            switch ((OpCodes) theOp.opCode.Value)
            {
                case OpCodes.Ldloc:
                case OpCodes.Ldloca:
                    localIndex = (ushort) Utilities.ReadInt16(theOp.ValueBytes, 0);
                    break;
                case OpCodes.Ldloc_0:
                    localIndex = 0;
                    break;
                case OpCodes.Ldloc_1:
                    localIndex = 1;
                    break;
                case OpCodes.Ldloc_2:
                    localIndex = 2;
                    break;
                case OpCodes.Ldloc_3:
                    localIndex = 3;
                    break;
                case OpCodes.Ldloc_S:
                case OpCodes.Ldloca_S:
                    localIndex = (ushort) theOp.ValueBytes[0];
                    break;
            }

            VariableInfo theLoc = conversionState.Input.TheMethodInfo.LocalInfos.ElementAt(localIndex);

            if (loadAddr)
            {
                conversionState.CurrentStackFrame.GetStack(theOp).Push(new StackItem()
                {
                    isFloat = false,
                    sizeOnStackInBytes = 4,
                    isGCManaged = false,
                    isValue = false
                });
            }
            else
            {
                int pushedLocalSizeVal = theLoc.TheTypeInfo.SizeOnStackInBytes;

                conversionState.CurrentStackFrame.GetStack(theOp).Push(new StackItem()
                {
                    isFloat = Utilities.IsFloat(theLoc.UnderlyingType),
                    sizeOnStackInBytes = pushedLocalSizeVal,
                    isGCManaged = theLoc.TheTypeInfo.IsGCManaged,
                    isValue = theLoc.TheTypeInfo.IsValueType
                });
            }
        }

        /// <summary>
        ///     See base class documentation.
        /// </summary>
        /// <param name="theOp">See base class documentation.</param>
        /// <param name="conversionState">See base class documentation.</param>
        /// <returns>See base class documentation.</returns>
        /// <exception cref="System.NotSupportedException">
        ///     Thrown when loading a float local is required as it currently hasn't been
        ///     implemented.
        ///     Also thrown if arguments are not of size 4 or 8 bytes.
        /// </exception>
        public override void Convert(ILConversionState conversionState, ILOp theOp)
        {
            //Load local

            bool loadAddr = (OpCodes) theOp.opCode.Value == OpCodes.Ldloca ||
                            (OpCodes) theOp.opCode.Value == OpCodes.Ldloca_S;
            ushort localIndex = 0;
            switch ((OpCodes) theOp.opCode.Value)
            {
                case OpCodes.Ldloc:
                case OpCodes.Ldloca:
                    localIndex = (ushort) Utilities.ReadInt16(theOp.ValueBytes, 0);
                    break;
                case OpCodes.Ldloc_0:
                    localIndex = 0;
                    break;
                case OpCodes.Ldloc_1:
                    localIndex = 1;
                    break;
                case OpCodes.Ldloc_2:
                    localIndex = 2;
                    break;
                case OpCodes.Ldloc_3:
                    localIndex = 3;
                    break;
                case OpCodes.Ldloc_S:
                case OpCodes.Ldloca_S:
                    localIndex = (ushort) theOp.ValueBytes[0];
                    break;
            }

            VariableInfo theLoc = conversionState.Input.TheMethodInfo.LocalInfos[localIndex];
            if (Utilities.IsFloat(theLoc.UnderlyingType))
            {
                //SUPPORT - floats
                throw new NotSupportedException("Float locals not supported yet!");
            }

            if (loadAddr)
            {
                conversionState.Append(new Mov() {Size = OperandSize.Dword, Src = "EBP", Dest = "EAX"});
                conversionState.Append(new ASMOps.Sub() {Src = (-theLoc.Offset).ToString(), Dest = "EAX"});
                conversionState.Append(new Push() {Size = OperandSize.Dword, Src = "EAX"});

                conversionState.CurrentStackFrame.GetStack(theOp).Push(new StackItem()
                {
                    isFloat = false,
                    sizeOnStackInBytes = 4,
                    isGCManaged = false,
                    isValue = false
                });
            }
            else
            {
                int localSizeOnStack = theLoc.TheTypeInfo.SizeOnStackInBytes;

                if (localSizeOnStack%4 != 0)
                {
                    throw new NotSupportedException("Invalid local bytes size!");
                }
                else
                {
                    for (int i = theLoc.Offset + (localSizeOnStack - 4); i >= theLoc.Offset; i -= 4)
                    {
                        conversionState.Append(new Push()
                        {
                            Size = OperandSize.Dword,
                            Src = "[EBP-" + Math.Abs(i).ToString() + "]"
                        });
                    }
                }

                conversionState.CurrentStackFrame.GetStack(theOp).Push(new StackItem()
                {
                    isFloat = Utilities.IsFloat(theLoc.UnderlyingType),
                    sizeOnStackInBytes = localSizeOnStack,
                    isGCManaged = theLoc.TheTypeInfo.IsGCManaged,
                    isValue = theLoc.TheTypeInfo.IsValueType
                });
            }
        }
    }
}