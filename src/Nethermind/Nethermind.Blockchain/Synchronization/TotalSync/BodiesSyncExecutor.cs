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

using System.Threading;
using System.Threading.Tasks;
using Nethermind.Blockchain.Synchronization.FastBlocks;
using Nethermind.Core;
using Nethermind.Logging;

namespace Nethermind.Blockchain.Synchronization.TotalSync
{
    public class BodiesSyncExecutor : SyncExecutor<BodiesSyncBatch>
    {
        public BodiesSyncExecutor(
            ISyncFeed<BodiesSyncBatch> syncFeed,
            IEthSyncPeerPool syncPeerPool,
            IPeerSelectionStrategyFactory<BodiesSyncBatch> peerSelectionStrategy,
            ILogManager logManager)
            : base(syncFeed, syncPeerPool, peerSelectionStrategy, logManager)
        {
        }

        protected override async Task Execute(PeerInfo peerInfo, BodiesSyncBatch batch, CancellationToken cancellationToken)
        {
            ISyncPeer peer = peerInfo.SyncPeer;
            batch.MarkSent();
            Task<BlockBody[]> getBodiesTask = peer.GetBlockBodies(batch.Request, cancellationToken);
            await getBodiesTask.ContinueWith(
                (t, state) =>
                {
                    BodiesSyncBatch batchLocal = (BodiesSyncBatch)state;
                    if (t.IsCompletedSuccessfully)
                    {
                        if (batchLocal.RequestTime > 1000)
                        {
                            if (Logger.IsDebug) Logger.Debug($"{batchLocal} - peer is slow {batchLocal.RequestTime:F2}");
                        }

                        batchLocal.Response = t.Result;
                    }
                }, batch);
        }
    }
}