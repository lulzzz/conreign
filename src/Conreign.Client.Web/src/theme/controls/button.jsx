import React, { PropTypes } from 'react';
import bem from 'bem-cn';

import {
  withThemeColors,
  withActiveState,
  withThemeSizes,
  decorate,
} from './decorators';

const CSS_CLASS = 'c-button';
const css = bem(CSS_CLASS);

function Button({
  className,
  children,
  fullWidth,
  ghost,
  rounded,
  close,
  ...others
}) {
  const modifiers = {
    block: fullWidth,
    ghost,
    rounded,
    close,
  };
  return (
    <button className={css(modifiers).mix(className)()} {...others}>
      {children}
    </button>
  );
}

export default decorate([
  withActiveState(CSS_CLASS),
  withThemeColors(
    CSS_CLASS,
    {
      getClassName: props => props.ghost
        ? `${CSS_CLASS}--ghost-${props.themeColor}`
        : `${CSS_CLASS}--${props.themeColor}`,
    },
  ),
  withThemeSizes(),
])(Button);


Button.propTypes = {
  className: PropTypes.string,
  children: PropTypes.node,
  fullWidth: PropTypes.bool,
  ghost: PropTypes.bool,
  rounded: PropTypes.bool,
  close: PropTypes.bool,
};

Button.defaultProps = {
  className: null,
  children: null,
  fullWidth: false,
  ghost: false,
  rounded: false,
  close: false,
};