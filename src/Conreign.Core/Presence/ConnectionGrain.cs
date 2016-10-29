using System;
using System.Threading.Tasks;
using Conreign.Core.Communication;
using Conreign.Core.Contracts.Communication;
using Conreign.Core.Contracts.Presence;
using Orleans;

namespace Conreign.Core.Presence
{
    public class ConnectionGrain : Grain<ConnectionState>, IConnectionGrain
    {
        private Connection _connection;

        public override async Task OnActivateAsync()
        {
            State.ConnectionId = this.GetPrimaryKey();
            _connection = new Connection(State, this);
            await base.OnActivateAsync();
        }

        public Task Connect(string topicId)
        {
            return _connection.Connect(topicId);
        }

        public async Task Disconnect()
        {
            await _connection.Disconnect();
            DeactivateOnIdle();
        }

        public Task<ITopic> Create(string id)
        {
            var topic = new Topic(GetStreamProvider(StreamConstants.ProviderName), id);
            return Task.FromResult((ITopic)topic);
        }
    }
}