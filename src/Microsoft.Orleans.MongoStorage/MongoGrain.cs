﻿using MongoDB.Bson.Serialization.Attributes;

namespace Microsoft.Orleans.Storage
{
    internal class MongoGrain
    {
        [BsonId]
        public string Id { get; set; }

        [BsonRequired]
        [BsonElement("meta")]
        public MongoGrainMeta Meta { get; set; }

        [BsonElement("data")]
        public object Data { get; set; }
    }
}