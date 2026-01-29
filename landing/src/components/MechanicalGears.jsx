import { useEffect, useRef } from 'react'
import './MechanicalGears.css'

/**
 * Mechanical Gears Component
 * Brass clockwork gears with the Tarot: Arcana Circle aesthetic
 *
 * @param {string} size - 'sm' | 'md' | 'lg'
 * @param {string} position - CSS position values
 * @param {number} opacity - 0-1
 */
export default function MechanicalGears({
  size = 'md',
  className = '',
  style = {}
}) {
  return (
    <div className={`gearbox gearbox-${size} ${className}`} style={style} aria-hidden="true">
      <div className="gear gear-one">
        <div className="gear-inner">
          <div className="bar"></div>
          <div className="bar"></div>
          <div className="bar"></div>
        </div>
      </div>
      <div className="gear gear-two">
        <div className="gear-inner">
          <div className="bar"></div>
          <div className="bar"></div>
          <div className="bar"></div>
        </div>
      </div>
      <div className="gear gear-three">
        <div className="gear-inner">
          <div className="bar"></div>
          <div className="bar"></div>
          <div className="bar"></div>
        </div>
      </div>
      <div className="gear gear-four gear-large">
        <div className="gear-inner">
          <div className="bar"></div>
          <div className="bar"></div>
          <div className="bar"></div>
          <div className="bar"></div>
          <div className="bar"></div>
          <div className="bar"></div>
        </div>
      </div>
    </div>
  )
}

/**
 * Single Gear Component for decorative use
 */
export function SingleGear({
  size = 60,
  teeth = 3,
  direction = 'clockwise',
  speed = 4,
  className = '',
  style = {}
}) {
  const bars = Array.from({ length: teeth }, (_, i) => i)

  return (
    <div
      className={`single-gear ${className}`}
      style={{
        '--gear-size': `${size}px`,
        '--gear-speed': `${speed}s`,
        '--gear-direction': direction === 'clockwise' ? 'normal' : 'reverse',
        ...style
      }}
      aria-hidden="true"
    >
      <div className="gear-inner">
        {bars.map((_, i) => (
          <div key={i} className="bar" style={{ '--bar-index': i, '--bar-count': teeth }} />
        ))}
      </div>
    </div>
  )
}

/**
 * Interlocking Gears Decoration
 */
export function GearDecoration({ position = 'top-right', opacity = 0.15 }) {
  const positionStyles = {
    'top-right': { top: '-50px', right: '-50px' },
    'top-left': { top: '-50px', left: '-50px' },
    'bottom-right': { bottom: '-50px', right: '-50px' },
    'bottom-left': { bottom: '-50px', left: '-50px' },
  }

  return (
    <div
      className="gear-decoration"
      style={{
        position: 'absolute',
        opacity,
        ...positionStyles[position]
      }}
    >
      <MechanicalGears size="sm" />
    </div>
  )
}
