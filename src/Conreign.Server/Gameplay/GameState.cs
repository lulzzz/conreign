﻿using System;
using System.Collections.Generic;
using Conreign.Contracts.Gameplay.Data;
using Conreign.Server.Presence;

namespace Conreign.Server.Gameplay
{
    public class GameState
    {
        public string RoomId { get; set; }
        public bool IsEnded { get; set; }
        public bool IsStarted { get; set; }
        public int Turn { get; set; }
        public MapData Map { get; set; } = new MapData();
        public List<PlayerData> Players { get; set; } = new List<PlayerData>();
        public HubState Hub { get; set; } = new HubState();
        public Dictionary<Guid, PlayerGameState> PlayerStates { get; set; } = new Dictionary<Guid, PlayerGameState>();
    }
}