import React from 'react';
import PropTypes from 'prop-types';

import { P, Text, ThemeColor } from './../../theme';

export const WELCOME_MESSAGE_RECEIVED = 'WelcomeMessageReceived';

export function createWelcomeMessageEvent() {
  return {
    type: WELCOME_MESSAGE_RECEIVED,
    payload: {
      senderId: null,
      timestamp: new Date().toISOString(),
      link: window.location.href,
    },
  };
}

export function WelcomeMessageEvent({ link }) {
  return [
    <P key={1}>
      Nice to meet you, commander! Invite some friends to join the battle at
      {' '}
      <a href={link}>{link}</a>
    </P>,
    <P key={2}>
      <Text themeColor={ThemeColor.Brand}>Hint:</Text>
      {' '}
      You can change your name by pressing the settings cog at the bottom of this chat.
    </P>,
  ];
}

WelcomeMessageEvent.propTypes = {
  link: PropTypes.string.isRequired,
};

export default WelcomeMessageEvent;
