﻿//  Copyright (c) 2018 Demerzel Solutions Limited
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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nethermind.Core2;
using Nethermind.Core2.Configuration;
using Nethermind.Core2.Containers;
using Nethermind.Core2.Crypto;
using Nethermind.Core2.Types;

namespace Nethermind.BeaconNode.Storage
{
    // Data Class
    public class MemoryStore : IStore
    {
        private readonly Dictionary<Root, BeaconBlock> _blocks = new Dictionary<Root, BeaconBlock>();
        private readonly Dictionary<Root, BeaconState> _blockStates = new Dictionary<Root, BeaconState>();
        private readonly Dictionary<Checkpoint, BeaconState> _checkpointStates = new Dictionary<Checkpoint, BeaconState>();
        private readonly Dictionary<ValidatorIndex, LatestMessage> _latestMessages = new Dictionary<ValidatorIndex, LatestMessage>();
        private readonly ILogger _logger;
        private readonly IOptionsMonitor<TimeParameters> _timeParameterOptions;

        public MemoryStore(ILogger<MemoryStore> logger,
            IOptionsMonitor<TimeParameters> timeParameterOptions)
        {
            _logger = logger;
            _timeParameterOptions = timeParameterOptions;
        }

        public Checkpoint BestJustifiedCheckpoint { get; private set; } = Checkpoint.Zero;
        public Checkpoint FinalizedCheckpoint { get; private set; } = Checkpoint.Zero;
        public ulong GenesisTime { get; private set; }
        public bool IsInitialized { get; private set; }
        public Checkpoint JustifiedCheckpoint { get; private set; } = Checkpoint.Zero;
        public ulong Time { get; private set; }

        public Task InitializeForkChoiceStoreAsync(ulong time, ulong genesisTime, Checkpoint justifiedCheckpoint,
            Checkpoint finalizedCheckpoint, Checkpoint bestJustifiedCheckpoint, IDictionary<Root, BeaconBlock> blocks, IDictionary<Root, BeaconState> states,
            IDictionary<Checkpoint, BeaconState> checkpointStates)
        {
            if (IsInitialized)
            {
                throw new Exception("Store already initialized.");
            }
            
            Time = time;
            GenesisTime = genesisTime;
            JustifiedCheckpoint = justifiedCheckpoint;
            FinalizedCheckpoint = finalizedCheckpoint;
            BestJustifiedCheckpoint = bestJustifiedCheckpoint;
            foreach (KeyValuePair<Root, BeaconBlock> kvp in blocks)
            {
                _blocks[kvp.Key] = kvp.Value;
            }
            foreach (KeyValuePair<Root, BeaconState> kvp in states)
            {
                _blockStates[kvp.Key] = kvp.Value;
            }
            foreach (KeyValuePair<Checkpoint, BeaconState> kvp in checkpointStates)
            {
                _checkpointStates[kvp.Key] = kvp.Value;
            }
            IsInitialized = true;
            return Task.CompletedTask;
        }

        public Task SetBestJustifiedCheckpointAsync(Checkpoint checkpoint)
        {
            BestJustifiedCheckpoint = checkpoint;
            return Task.CompletedTask;
        }

        public Task SetBlockAsync(Root blockHashTreeRoot, BeaconBlock beaconBlock)
        {
            _blocks[blockHashTreeRoot] = beaconBlock;
            return Task.CompletedTask;
        }

        public Task SetBlockStateAsync(Root blockHashTreeRoot, BeaconState beaconState)
        {
            _blockStates[blockHashTreeRoot] = beaconState;
            return Task.CompletedTask;
        }

        public Task SetCheckpointStateAsync(Checkpoint checkpoint, BeaconState state)
        {
            _checkpointStates[checkpoint] = state;
            return Task.CompletedTask;
        }

        public Task SetFinalizedCheckpointAsync(Checkpoint checkpoint)
        {
            FinalizedCheckpoint = checkpoint;
            return Task.CompletedTask;
        }

        public Task SetJustifiedCheckpointAsync(Checkpoint checkpoint)
        {
            JustifiedCheckpoint = checkpoint;
            return Task.CompletedTask;
        }

        public Task SetLatestMessageAsync(ValidatorIndex validatorIndex, LatestMessage latestMessage)
        {
            _latestMessages[validatorIndex] = latestMessage;
            return Task.CompletedTask;
        }

        public Task SetTimeAsync(ulong time)
        {
            Time = time;
            return Task.CompletedTask;
        }

        public ValueTask<BeaconBlock> GetBlockAsync(Root blockRoot)
        {
            if (!_blocks.TryGetValue(blockRoot, out BeaconBlock? beaconBlock))
            {
                throw new ArgumentOutOfRangeException(nameof(blockRoot), blockRoot, "Block not found in store.");
            }
            return new ValueTask<BeaconBlock>(beaconBlock!);
        }
        
        public ValueTask<BeaconState> GetBlockStateAsync(Root blockRoot)
        {
            if (!_blockStates.TryGetValue(blockRoot, out BeaconState? state))
            {
                throw new ArgumentOutOfRangeException(nameof(blockRoot), blockRoot, "State not found in store.");
            }
            return new ValueTask<BeaconState>(state!);
        }

        public ValueTask<BeaconState?> GetCheckpointStateAsync(Checkpoint checkpoint, bool throwIfMissing)
        {
            if (!_checkpointStates.TryGetValue(checkpoint, out BeaconState? state))
            {
                if (throwIfMissing)
                {
                    throw new ArgumentOutOfRangeException(nameof(checkpoint), checkpoint,
                        "Checkpoint state not found in store."); 
                }
            }
            return new ValueTask<BeaconState?>(state);
        }

        public async IAsyncEnumerable<Root> GetChildKeysAsync(Root parent)
        {
            await Task.CompletedTask;
            IEnumerable<Root> childKeys = _blocks
                .Where(kvp =>
                    kvp.Value.ParentRoot.Equals(parent))
                .Select(kvp => kvp.Key);
            foreach (Root childKey in childKeys)
            {
                yield return childKey;
            }
        }
        
        public async IAsyncEnumerable<Root> GetChildKeysAfterSlotAsync(Root parent, Slot slot)
        {
            await Task.CompletedTask;
            IEnumerable<Root> childKeys = _blocks
                .Where(kvp =>
                    kvp.Value.ParentRoot.Equals(parent)
                    && kvp.Value.Slot > slot)
                .Select(kvp => kvp.Key);
            foreach (Root childKey in childKeys)
            {
                yield return childKey;
            }
        }

        public ValueTask<LatestMessage?> GetLatestMessageAsync(ValidatorIndex validatorIndex, bool throwIfMissing)
        {
            if (!_latestMessages.TryGetValue(validatorIndex, out LatestMessage? latestMessage))
            {
                if (throwIfMissing)
                {
                    throw new ArgumentOutOfRangeException(nameof(validatorIndex), validatorIndex,
                        "Latest message not found in store.");
                }
            }
            return new ValueTask<LatestMessage?>(latestMessage);
        }
    }
}
