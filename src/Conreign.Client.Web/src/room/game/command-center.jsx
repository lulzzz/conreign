import React, { PropTypes } from 'react';

import { PLANET_SHAPE, PLAYER_SHAPE } from './../room-schemas';
import { Deck, DeckItem } from './../../theme';
import FleetRouteCard from './fleet-route-card';
import FleetForm from './fleet-form';

function CommandCenter({
  currentPlayer,
  sourcePlanet,
  destinationPlanet,
  destinationPlanetOwner,
  routeDistance,
}) {
  return (
    <Deck>
      <DeckItem>
        <FleetRouteCard
          sourcePlanet={sourcePlanet}
          sourcePlanetOwner={currentPlayer}
          destinationPlanet={destinationPlanet}
          destinationPlanetOwner={destinationPlanetOwner}
          currentUserId={currentPlayer.userId}
        />
      </DeckItem>
      {
        destinationPlanet && (
          <DeckItem>
            <FleetForm
              maxShips={sourcePlanet.ships}
              distance={routeDistance}
            />
          </DeckItem>
        )
      }
    </Deck>
  );
}

CommandCenter.propTypes = {
  currentPlayer: PLAYER_SHAPE.isRequired,
  sourcePlanet: PLANET_SHAPE.isRequired,
  destinationPlanet: PLANET_SHAPE,
  destinationPlanetOwner: PLAYER_SHAPE,
  routeDistance: PropTypes.number,
};

CommandCenter.defaultProps = {
  destinationPlanet: null,
  destinationPlanetOwner: null,
  routeDistance: null,
};

export default CommandCenter;
