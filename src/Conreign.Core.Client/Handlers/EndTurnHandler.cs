﻿using System.Threading.Tasks;
using Conreign.Core.Contracts.Client.Messages;
using MediatR;

namespace Conreign.Core.Client.Handlers
{
    internal class EndTurnHandler : ICommandHandler<EndTurnCommand, Unit>
    {
        public async Task<Unit> Handle(CommandEnvelope<EndTurnCommand, Unit> message)
        {
            var context = message.Context;
            var command = message.Command;
            var player = await context.User.JoinRoom(command.RoomId, context.Connection.Id);
            await player.EndTurn();
            return Unit.Value;
        }
    }
}
