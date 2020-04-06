//  Copyright (c) 2018 Demerzel Solutions Limited
//  This file is part of the Nethermind library.
// 
//  The Nethermind library is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  The Nethermind library is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public License
//  along with the Nethermind. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Linq;
using Nethermind.Core;
using Nethermind.Core.Crypto;

namespace Nethermind.Blockchain.Synchronization.FastBlocks
{
    public class ReceiptsSyncBatch : FastBlocksBatch
    {
        public long StartNumber => Predecessors.Length > 0 ? (Predecessors[^1] ?? long.MaxValue) : long.MaxValue;
        public long EndNumber => Blocks.First().Number;
        public bool IsFinal { get; set; }
        public long?[] Predecessors { get; set; }
        public Block[] Blocks { get; set; }
        public Keccak[] Request { get; set; }
        public TxReceipt[][] Response { get; set; }
        public override bool IsResponseEmpty => Response == null;
        
        public void Resize(int targetSize)
        {
            Block[] currentBlocks = Blocks;
            Keccak[] currentRequests = Request;
            Blocks = new Block[targetSize];
            Request = new Keccak[targetSize];
            Array.Copy(currentBlocks, 0, Blocks, 0, targetSize);
            Array.Copy(currentRequests, 0, Request, 0, targetSize);
        }
        
        public override string ToString()
        {
            string details = $"[{StartNumber}, {EndNumber}]({Blocks.Length})";
            return $"RECEIPTS {details} [{(Prioritized ? "HIGH" : "LOW")}] [times: S:{SchedulingTime:F0}ms|R:{RequestTime:F0}ms|V:{ValidationTime:F0}ms|W:{WaitingTime:F0}ms|H:{HandlingTime:F0}ms|A:{AgeInMs:F0}ms, retries {Retries}] min#: {MinNumber} {ResponseSourcePeer}";
        }
    }
}