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
using Nethermind.Blockchain.Synchronization.TotalSync;

namespace Nethermind.Blockchain.Synchronization
{
    internal class BlocksSyncPeerSelectionStrategyFactory : IPeerSelectionStrategyFactory<BlocksRequest>
    {
        private readonly IBlockTree _blockTree;

        public BlocksSyncPeerSelectionStrategyFactory(IBlockTree blockTree)
        {
            _blockTree = blockTree ?? throw new ArgumentNullException(nameof(blockTree));
        }
        
        public IPeerSelectionStrategy Create(BlocksRequest request)
        {
            IPeerSelectionStrategy baseStrategy = new BlocksSyncPeerSelectionStrategy(request.NumberOfLatestBlocksToBeIgnored);
            TotalDiffStrategy totalDiffStrategy = new TotalDiffStrategy(baseStrategy, (_blockTree.BestSuggestedHeader?.TotalDifficulty + 1) ?? 0);
            return totalDiffStrategy;
        }
    }
}