﻿using System;
using System.Threading.Tasks;
using Conreign.Core.Contracts.Communication;
using Conreign.Core.Contracts.Communication.Events;
using Conreign.Core.Contracts.Presence;

namespace Conreign.Core.Presence
{
    public class Connection : IConnection
    {
        private readonly ConnectionState _state;
        private readonly ITopicFactory _topicFactory;

        public Connection(ConnectionState state, ITopicFactory topicFactory)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }
            if (topicFactory == null)
            {
                throw new ArgumentNullException(nameof(topicFactory));
            }
            _state = state;
            _topicFactory = topicFactory;
        }

        public async Task Connect(string topicId)
        {
            if (string.IsNullOrEmpty(topicId))
            {
                throw new ArgumentException("Topic id cannot be null or empty.", nameof(topicId));
            }
            if (_state.TopicId == topicId)
            {
                return;
            }
            var previousTopicId = _state.TopicId;
            ITopic topic;
            if (previousTopicId != null)
            {
                topic = await _topicFactory.Create(previousTopicId);
                await topic.Send(new Disconnected(_state.ConnectionId));
            }
            _state.TopicId = topicId;
            topic = await _topicFactory.Create(topicId);
            await topic.Send(new Connected(_state.ConnectionId, topicId));
        }

        public async Task Disconnect()
        {
            if (_state.TopicId == null)
            {
                return;
            }
            var topic = await _topicFactory.Create(_state.TopicId);
            var @event = new Disconnected(_state.ConnectionId);
            await topic.Send(@event);
            _state.TopicId = null;
        }
    }
}