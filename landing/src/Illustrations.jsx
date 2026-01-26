// Complex SVG Illustrations for Tarot: Arcana Circle

// Sacred Geometry Arcana Circle - Main hero illustration
export function ArcanaCircle({ size = 500, className = '' }) {
  return (
    <svg
      viewBox="0 0 500 500"
      width={size}
      height={size}
      className={`arcana-svg ${className}`}
      xmlns="http://www.w3.org/2000/svg"
    >
      <defs>
        {/* Gradients */}
        <radialGradient id="coreGlow" cx="50%" cy="50%" r="50%">
          <stop offset="0%" stopColor="#4090ff" stopOpacity="0.4" />
          <stop offset="70%" stopColor="#4090ff" stopOpacity="0.1" />
          <stop offset="100%" stopColor="#4090ff" stopOpacity="0" />
        </radialGradient>

        <radialGradient id="brassGlow" cx="50%" cy="50%" r="50%">
          <stop offset="0%" stopColor="#c0a060" stopOpacity="0.5" />
          <stop offset="100%" stopColor="#c0a060" stopOpacity="0" />
        </radialGradient>

        <linearGradient id="brassGrad" x1="0%" y1="0%" x2="100%" y2="100%">
          <stop offset="0%" stopColor="#d4bc82" />
          <stop offset="50%" stopColor="#c0a060" />
          <stop offset="100%" stopColor="#8f743e" />
        </linearGradient>

        <linearGradient id="sapphireGrad" x1="0%" y1="0%" x2="100%" y2="100%">
          <stop offset="0%" stopColor="#60a8ff" />
          <stop offset="100%" stopColor="#4090ff" />
        </linearGradient>

        {/* Filters */}
        <filter id="glow" x="-50%" y="-50%" width="200%" height="200%">
          <feGaussianBlur stdDeviation="3" result="coloredBlur" />
          <feMerge>
            <feMergeNode in="coloredBlur" />
            <feMergeNode in="SourceGraphic" />
          </feMerge>
        </filter>

        <filter id="strongGlow" x="-50%" y="-50%" width="200%" height="200%">
          <feGaussianBlur stdDeviation="6" result="coloredBlur" />
          <feMerge>
            <feMergeNode in="coloredBlur" />
            <feMergeNode in="SourceGraphic" />
          </feMerge>
        </filter>

        {/* Patterns */}
        <pattern id="runePattern" patternUnits="userSpaceOnUse" width="40" height="40">
          <circle cx="20" cy="20" r="1" fill="#c0a060" opacity="0.3" />
        </pattern>
      </defs>

      {/* Background glow */}
      <circle cx="250" cy="250" r="240" fill="url(#coreGlow)" className="pulse-slow" />

      {/* Outer decorative ring */}
      <g className="spin-slow">
        <circle cx="250" cy="250" r="235" fill="none" stroke="#c0a060" strokeWidth="0.5" opacity="0.3" />
        <circle cx="250" cy="250" r="230" fill="none" stroke="#4090ff" strokeWidth="1" strokeDasharray="4 8" opacity="0.4" />

        {/* Outer tick marks */}
        {[...Array(72)].map((_, i) => (
          <line
            key={`tick-${i}`}
            x1="250"
            y1="15"
            x2="250"
            y2={i % 6 === 0 ? "25" : "20"}
            stroke={i % 6 === 0 ? "#c0a060" : "#4090ff"}
            strokeWidth={i % 6 === 0 ? "2" : "1"}
            opacity={i % 6 === 0 ? "0.8" : "0.4"}
            transform={`rotate(${i * 5} 250 250)`}
          />
        ))}
      </g>

      {/* Sacred geometry - outer hexagram */}
      <g className="spin-reverse" filter="url(#glow)">
        <polygon
          points="250,50 380,175 380,325 250,450 120,325 120,175"
          fill="none"
          stroke="url(#brassGrad)"
          strokeWidth="1.5"
          opacity="0.6"
        />
        <polygon
          points="250,50 380,175 380,325 250,450 120,325 120,175"
          fill="none"
          stroke="#c0a060"
          strokeWidth="0.5"
          strokeDasharray="10 5"
          opacity="0.4"
          transform="rotate(30 250 250)"
        />
      </g>

      {/* Middle ring with runes */}
      <g className="spin-medium">
        <circle cx="250" cy="250" r="180" fill="none" stroke="#4090ff" strokeWidth="2" opacity="0.5" />
        <circle cx="250" cy="250" r="175" fill="none" stroke="#c0a060" strokeWidth="0.5" opacity="0.3" />

        {/* Arcane symbols on ring */}
        {['⬡', '◈', '⚔', '✦', '☉', '☽', '★', '◇'].map((symbol, i) => (
          <text
            key={`symbol-${i}`}
            x="250"
            y="78"
            textAnchor="middle"
            fill={i % 2 === 0 ? "#c0a060" : "#4090ff"}
            fontSize="16"
            fontFamily="serif"
            transform={`rotate(${i * 45} 250 250)`}
            filter="url(#glow)"
          >
            {symbol}
          </text>
        ))}
      </g>

      {/* Inner sacred geometry - Star of David pattern */}
      <g className="spin-slow" filter="url(#glow)">
        <polygon
          points="250,100 320,200 320,300 250,400 180,300 180,200"
          fill="none"
          stroke="url(#sapphireGrad)"
          strokeWidth="1"
          opacity="0.5"
        />
        <polygon
          points="250,100 320,200 320,300 250,400 180,300 180,200"
          fill="none"
          stroke="url(#sapphireGrad)"
          strokeWidth="1"
          opacity="0.5"
          transform="rotate(60 250 250)"
        />
      </g>

      {/* Intricate inner ring */}
      <g className="spin-reverse-medium">
        <circle cx="250" cy="250" r="130" fill="none" stroke="#c0a060" strokeWidth="1" strokeDasharray="1 3" opacity="0.6" />

        {/* Decorative nodes */}
        {[...Array(12)].map((_, i) => (
          <g key={`node-${i}`} transform={`rotate(${i * 30} 250 250)`}>
            <circle cx="250" cy="120" r="8" fill="none" stroke="#c0a060" strokeWidth="1" opacity="0.7" />
            <circle cx="250" cy="120" r="3" fill="#c0a060" opacity="0.8" />
            <line x1="250" y1="128" x2="250" y2="145" stroke="#c0a060" strokeWidth="0.5" opacity="0.5" />
          </g>
        ))}
      </g>

      {/* Orbiting particles */}
      <g className="spin-fast">
        {[...Array(8)].map((_, i) => (
          <circle
            key={`particle-${i}`}
            cx="250"
            cy="90"
            r="4"
            fill="#4090ff"
            filter="url(#strongGlow)"
            transform={`rotate(${i * 45} 250 250)`}
            className="twinkle"
            style={{ animationDelay: `${i * 0.3}s` }}
          />
        ))}
      </g>

      {/* Core circle with glass effect */}
      <circle cx="250" cy="250" r="100" fill="#121620" fillOpacity="0.9" />
      <circle cx="250" cy="250" r="100" fill="url(#coreGlow)" />
      <circle cx="250" cy="250" r="100" fill="none" stroke="url(#brassGrad)" strokeWidth="3" />
      <circle cx="250" cy="250" r="95" fill="none" stroke="#4090ff" strokeWidth="0.5" opacity="0.5" />

      {/* Inner core decorations */}
      <g className="spin-reverse-slow">
        {[...Array(4)].map((_, i) => (
          <g key={`core-dec-${i}`} transform={`rotate(${i * 90} 250 250)`}>
            <path
              d="M250,160 L260,180 L250,175 L240,180 Z"
              fill="#c0a060"
              opacity="0.8"
            />
          </g>
        ))}
      </g>

      {/* Center eye/lens */}
      <circle cx="250" cy="250" r="30" fill="#1a2030" />
      <circle cx="250" cy="250" r="30" fill="url(#brassGlow)" />
      <circle cx="250" cy="250" r="30" fill="none" stroke="#c0a060" strokeWidth="2" />
      <circle cx="250" cy="250" r="15" fill="none" stroke="#4090ff" strokeWidth="1" opacity="0.8" />
      <circle cx="250" cy="250" r="5" fill="#4090ff" filter="url(#strongGlow)" className="pulse" />
    </svg>
  )
}

// Elaborate Card Frame
export function CardFrame({ children, tribe = 'default', size = 200 }) {
  const colors = {
    Pentacles: { primary: '#c0a060', secondary: '#8f743e', glow: 'rgba(192, 160, 96, 0.5)' },
    Cups: { primary: '#4090ff', secondary: '#2060c0', glow: 'rgba(64, 144, 255, 0.5)' },
    Swords: { primary: '#a03030', secondary: '#702020', glow: 'rgba(160, 48, 48, 0.5)' },
    Wands: { primary: '#8b5cf6', secondary: '#6b3cd6', glow: 'rgba(139, 92, 246, 0.5)' },
    default: { primary: '#c0a060', secondary: '#8f743e', glow: 'rgba(192, 160, 96, 0.5)' }
  }

  const c = colors[tribe] || colors.default
  const w = size
  const h = size * 1.4

  return (
    <div className="card-frame-container" style={{ width: w, height: h }}>
      <svg viewBox="0 0 200 280" width={w} height={h} className="card-frame-svg">
        <defs>
          <linearGradient id={`cardGrad-${tribe}`} x1="0%" y1="0%" x2="100%" y2="100%">
            <stop offset="0%" stopColor={c.primary} />
            <stop offset="100%" stopColor={c.secondary} />
          </linearGradient>

          <filter id={`cardGlow-${tribe}`} x="-50%" y="-50%" width="200%" height="200%">
            <feGaussianBlur stdDeviation="4" result="coloredBlur" />
            <feMerge>
              <feMergeNode in="coloredBlur" />
              <feMergeNode in="SourceGraphic" />
            </feMerge>
          </filter>

          <clipPath id="cardClip">
            <rect x="10" y="10" width="180" height="260" rx="12" />
          </clipPath>
        </defs>

        {/* Outer glow */}
        <rect x="5" y="5" width="190" height="270" rx="15" fill={c.glow} filter={`url(#cardGlow-${tribe})`} />

        {/* Card background */}
        <rect x="10" y="10" width="180" height="260" rx="12" fill="#0a0c12" />

        {/* Inner border pattern */}
        <rect x="15" y="15" width="170" height="250" rx="10" fill="none" stroke={c.primary} strokeWidth="1" opacity="0.3" />

        {/* Corner ornaments */}
        {[[20, 20], [180, 20], [20, 260], [180, 260]].map(([x, y], i) => (
          <g key={`corner-${i}`} transform={`translate(${x}, ${y}) rotate(${i * 90})`}>
            <path d="M0,0 L15,0 L15,3 L3,3 L3,15 L0,15 Z" fill={c.primary} opacity="0.8" />
            <circle cx="8" cy="8" r="2" fill={c.primary} />
          </g>
        ))}

        {/* Top ornament */}
        <path d="M70,8 L100,2 L130,8" fill="none" stroke={c.primary} strokeWidth="2" />
        <circle cx="100" cy="5" r="4" fill={c.primary} />

        {/* Bottom ornament */}
        <path d="M70,272 L100,278 L130,272" fill="none" stroke={c.primary} strokeWidth="2" />
        <circle cx="100" cy="275" r="4" fill={c.primary} />

        {/* Side decorations */}
        <line x1="8" y1="80" x2="8" y2="200" stroke={c.primary} strokeWidth="1" strokeDasharray="4 4" opacity="0.5" />
        <line x1="192" y1="80" x2="192" y2="200" stroke={c.primary} strokeWidth="1" strokeDasharray="4 4" opacity="0.5" />

        {/* Main border */}
        <rect x="10" y="10" width="180" height="260" rx="12" fill="none" stroke={`url(#cardGrad-${tribe})`} strokeWidth="3" />
      </svg>
      <div className="card-frame-content">
        {children}
      </div>
    </div>
  )
}

// Decorative Section Divider
export function SectionDivider({ variant = 'default' }) {
  return (
    <svg viewBox="0 0 400 40" className="section-divider" preserveAspectRatio="xMidYMid meet">
      <defs>
        <linearGradient id="dividerGrad" x1="0%" y1="50%" x2="100%" y2="50%">
          <stop offset="0%" stopColor="#c0a060" stopOpacity="0" />
          <stop offset="30%" stopColor="#c0a060" stopOpacity="1" />
          <stop offset="50%" stopColor="#4090ff" stopOpacity="1" />
          <stop offset="70%" stopColor="#c0a060" stopOpacity="1" />
          <stop offset="100%" stopColor="#c0a060" stopOpacity="0" />
        </linearGradient>
      </defs>

      {/* Main line */}
      <line x1="0" y1="20" x2="400" y2="20" stroke="url(#dividerGrad)" strokeWidth="1" />

      {/* Center ornament */}
      <g transform="translate(200, 20)">
        <circle r="12" fill="#121620" stroke="#c0a060" strokeWidth="2" />
        <circle r="6" fill="none" stroke="#4090ff" strokeWidth="1" />
        <circle r="2" fill="#4090ff" />

        {/* Rays */}
        {[...Array(8)].map((_, i) => (
          <line
            key={i}
            x1="0"
            y1="-15"
            x2="0"
            y2="-25"
            stroke="#c0a060"
            strokeWidth="1"
            opacity="0.6"
            transform={`rotate(${i * 45})`}
          />
        ))}
      </g>

      {/* Side diamonds */}
      {[-100, 100].map((x, i) => (
        <g key={i} transform={`translate(${200 + x}, 20)`}>
          <polygon points="0,-6 6,0 0,6 -6,0" fill="#c0a060" opacity="0.8" />
        </g>
      ))}
    </svg>
  )
}

// Cosmic Background Elements
export function CosmicOrb({ x, y, size, color = '#4090ff', delay = 0 }) {
  return (
    <div
      className="cosmic-orb"
      style={{
        left: x,
        top: y,
        width: size,
        height: size,
        background: `radial-gradient(circle, ${color}40, transparent 70%)`,
        animationDelay: `${delay}s`
      }}
    />
  )
}

// Animated constellation lines
export function ConstellationBg() {
  const points = [
    { x: 10, y: 20 }, { x: 25, y: 15 }, { x: 40, y: 25 },
    { x: 55, y: 10 }, { x: 70, y: 30 }, { x: 85, y: 18 },
    { x: 15, y: 60 }, { x: 30, y: 70 }, { x: 50, y: 55 },
    { x: 65, y: 75 }, { x: 80, y: 65 }, { x: 90, y: 80 }
  ]

  const connections = [
    [0, 1], [1, 2], [2, 3], [3, 4], [4, 5],
    [6, 7], [7, 8], [8, 9], [9, 10], [10, 11],
    [1, 7], [2, 8], [4, 10]
  ]

  return (
    <svg viewBox="0 0 100 100" className="constellation-bg" preserveAspectRatio="none">
      <defs>
        <filter id="starGlow">
          <feGaussianBlur stdDeviation="0.5" result="coloredBlur" />
          <feMerge>
            <feMergeNode in="coloredBlur" />
            <feMergeNode in="SourceGraphic" />
          </feMerge>
        </filter>
      </defs>

      {/* Connection lines */}
      {connections.map(([a, b], i) => (
        <line
          key={`line-${i}`}
          x1={points[a].x}
          y1={points[a].y}
          x2={points[b].x}
          y2={points[b].y}
          stroke="#4090ff"
          strokeWidth="0.1"
          opacity="0.3"
          className="constellation-line"
          style={{ animationDelay: `${i * 0.2}s` }}
        />
      ))}

      {/* Stars */}
      {points.map((p, i) => (
        <circle
          key={`star-${i}`}
          cx={p.x}
          cy={p.y}
          r={i % 3 === 0 ? 0.8 : 0.4}
          fill={i % 2 === 0 ? '#4090ff' : '#c0a060'}
          filter="url(#starGlow)"
          className="constellation-star"
          style={{ animationDelay: `${i * 0.15}s` }}
        />
      ))}
    </svg>
  )
}

// Tribe Symbol with intricate design
export function TribeSymbol({ tribe, size = 80 }) {
  const configs = {
    Pentacles: {
      color: '#c0a060',
      path: 'M40,10 L50,35 L75,35 L55,50 L65,75 L40,60 L15,75 L25,50 L5,35 L30,35 Z',
      inner: 'M40,25 L45,38 L58,38 L48,47 L52,60 L40,52 L28,60 L32,47 L22,38 L35,38 Z'
    },
    Cups: {
      color: '#4090ff',
      path: 'M25,15 L55,15 L55,25 L60,25 L60,45 C60,65 40,75 40,75 C40,75 20,65 20,45 L20,25 L25,25 Z',
      inner: 'M30,25 L50,25 L50,45 C50,55 40,62 40,62 C40,62 30,55 30,45 Z'
    },
    Swords: {
      color: '#a03030',
      path: 'M40,5 L45,30 L55,30 L40,75 L25,30 L35,30 Z M20,25 L60,25 L60,30 L20,30 Z',
      inner: 'M40,15 L43,35 L37,35 Z'
    },
    Wands: {
      color: '#8b5cf6',
      path: 'M37,10 L43,10 L43,55 L50,55 L40,75 L30,55 L37,55 Z',
      inner: 'M40,20 L42,50 L38,50 Z'
    }
  }

  const config = configs[tribe] || configs.Pentacles

  return (
    <svg viewBox="0 0 80 80" width={size} height={size} className="tribe-symbol-svg">
      <defs>
        <linearGradient id={`tribe-grad-${tribe}`} x1="0%" y1="0%" x2="100%" y2="100%">
          <stop offset="0%" stopColor={config.color} />
          <stop offset="100%" stopColor={config.color} stopOpacity="0.6" />
        </linearGradient>
        <filter id={`tribe-glow-${tribe}`} x="-50%" y="-50%" width="200%" height="200%">
          <feGaussianBlur stdDeviation="3" result="coloredBlur" />
          <feMerge>
            <feMergeNode in="coloredBlur" />
            <feMergeNode in="SourceGraphic" />
          </feMerge>
        </filter>
      </defs>

      {/* Glow background */}
      <circle cx="40" cy="40" r="35" fill={config.color} fillOpacity="0.1" />

      {/* Outer ring */}
      <circle cx="40" cy="40" r="38" fill="none" stroke={config.color} strokeWidth="1" opacity="0.5" />
      <circle cx="40" cy="40" r="35" fill="none" stroke={config.color} strokeWidth="0.5" strokeDasharray="2 2" opacity="0.3" />

      {/* Main symbol */}
      <path d={config.path} fill={`url(#tribe-grad-${tribe})`} filter={`url(#tribe-glow-${tribe})`} />

      {/* Inner detail */}
      <path d={config.inner} fill="#121620" fillOpacity="0.5" />
    </svg>
  )
}

// Mechanical gear decoration
export function GearDecoration({ size = 100, className = '' }) {
  return (
    <svg viewBox="0 0 100 100" width={size} height={size} className={`gear-decoration ${className}`}>
      <defs>
        <linearGradient id="gearGrad" x1="0%" y1="0%" x2="100%" y2="100%">
          <stop offset="0%" stopColor="#c0a060" />
          <stop offset="100%" stopColor="#8f743e" />
        </linearGradient>
      </defs>

      {/* Gear teeth */}
      <g className="gear-spin">
        {[...Array(12)].map((_, i) => (
          <rect
            key={i}
            x="46"
            y="5"
            width="8"
            height="12"
            rx="2"
            fill="url(#gearGrad)"
            transform={`rotate(${i * 30} 50 50)`}
          />
        ))}

        {/* Main gear body */}
        <circle cx="50" cy="50" r="35" fill="#1a2030" stroke="url(#gearGrad)" strokeWidth="3" />

        {/* Inner details */}
        <circle cx="50" cy="50" r="25" fill="none" stroke="#c0a060" strokeWidth="1" opacity="0.5" />
        <circle cx="50" cy="50" r="15" fill="none" stroke="#c0a060" strokeWidth="2" />
        <circle cx="50" cy="50" r="8" fill="#c0a060" />
        <circle cx="50" cy="50" r="4" fill="#1a2030" />

        {/* Spokes */}
        {[...Array(6)].map((_, i) => (
          <line
            key={`spoke-${i}`}
            x1="50"
            y1="20"
            x2="50"
            y2="35"
            stroke="#c0a060"
            strokeWidth="2"
            transform={`rotate(${i * 60} 50 50)`}
          />
        ))}
      </g>
    </svg>
  )
}

export default {
  ArcanaCircle,
  CardFrame,
  SectionDivider,
  CosmicOrb,
  ConstellationBg,
  TribeSymbol,
  GearDecoration
}
