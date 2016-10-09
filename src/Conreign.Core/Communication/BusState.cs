﻿using System;
using System.Collections.Generic;
using Conreign.Core.Contracts.Communication;
using Orleans.Streams;

namespace Conreign.Core.Communication
{
    public class BusState
    {
        public Guid StreamId { get; set; }
        public string Topic { get; set; }
        public Dictionary<Type, HashSet<IEventHandler<ISystemEvent>>> Subscribers { get; set; } = new Dictionary<Type, HashSet<IEventHandler<ISystemEvent>>>();
        public StreamSequenceToken StreamSequenceToken { get; set; }
    }
}