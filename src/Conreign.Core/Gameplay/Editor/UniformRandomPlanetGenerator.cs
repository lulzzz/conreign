﻿using System;
using Conreign.Core.Contracts.Gameplay.Data;

namespace Conreign.Core.Gameplay.Editor
{
    public class UniformRandomPlanetGenerator : IPlanetGenerator
    {
        private readonly UniformRandomPlanetGeneratorOptions _options;
        private readonly Random _random;

        public UniformRandomPlanetGenerator(UniformRandomPlanetGeneratorOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            _options = options;
            _random = new Random();
        }

        public PlanetData Generate(string name, Guid? ownerId)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name cannot be null or empty.", nameof(name));
            }
            var power = _random.Next(_options.MinPower, _options.MaxPower) / 100.0;
            var ships = _random.Next(_options.MinShips, _options.MaxShips);
            var production = _random.Next(_options.MinProductionRate, _options.MaxProductionRate);
            return new PlanetData
            {
                Name = name,
                OwnerId = ownerId,
                Power = power,
                Ships = ships,
                ProductionRate = production,
            };
        }
    }
}