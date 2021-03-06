﻿using Orleans.Concurrency;

namespace Conreign.Contracts.Communication
{
    [Immutable]
    public class EventEnvelope
    {
        public EventEnvelope(object payload, string type)
        {
            Payload = payload;
            Type = type;
        }

        public object Payload { get; }
        public string Type { get; }
    }
}