﻿using System.Threading.Tasks;
using Conreign.Client.Contracts.Messages;
using Conreign.Contracts.Gameplay.Data;

namespace Conreign.Client.Handler.Handlers
{
    internal class GetRoomStateHandler : ICommandHandler<GetRoomStateCommand, IRoomData>
    {
        public async Task<IRoomData> Handle(CommandEnvelope<GetRoomStateCommand, IRoomData> message)
        {
            var context = message.Context;
            var command = message.Command;
            var player = await context.User.JoinRoom(command.RoomId, context.Connection.Id);
            return await player.GetState();
        }
    }
}