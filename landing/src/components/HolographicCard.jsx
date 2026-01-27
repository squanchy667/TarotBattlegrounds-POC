import { useRef, useEffect, useState } from 'react'
import { motion } from 'framer-motion'
import './HolographicCard.css'

/**
 * Holographic Card Component
 * Pokemon-style holographic shine effect with Star-Forge branding
 *
 * Based on: https://codepen.io/NyX/pen/ydKWgM
 */
export default function HolographicCard({
  children,
  tribe = 'Pentacles',
  className = '',
  onClick,
  style = {}
}) {
  const cardRef = useRef(null)
  const [mousePosition, setMousePosition] = useState({ x: 50, y: 50 })
  const [isHovered, setIsHovered] = useState(false)

  const tribeColors = {
    Pentacles: { primary: '#C0A060', secondary: '#d4bc82', glow: 'rgba(192, 160, 96, 0.5)' },
    Cups: { primary: '#4090FF', secondary: '#60a8ff', glow: 'rgba(64, 144, 255, 0.5)' },
    Swords: { primary: '#A03030', secondary: '#c04040', glow: 'rgba(160, 48, 48, 0.5)' },
    Wands: { primary: '#8B5CF6', secondary: '#a78bfa', glow: 'rgba(139, 92, 246, 0.5)' },
  }

  const colors = tribeColors[tribe] || tribeColors.Pentacles

  const handleMouseMove = (e) => {
    if (!cardRef.current) return

    const rect = cardRef.current.getBoundingClientRect()
    const x = ((e.clientX - rect.left) / rect.width) * 100
    const y = ((e.clientY - rect.top) / rect.height) * 100

    setMousePosition({ x, y })
  }

  const handleMouseEnter = () => setIsHovered(true)
  const handleMouseLeave = () => {
    setIsHovered(false)
    setMousePosition({ x: 50, y: 50 })
  }

  // Calculate 3D rotation based on mouse position
  const rotateX = isHovered ? (mousePosition.y - 50) / 5 : 0
  const rotateY = isHovered ? (mousePosition.x - 50) / -5 : 0

  return (
    <motion.div
      ref={cardRef}
      className={`holo-card ${className}`}
      style={{
        '--mouse-x': `${mousePosition.x}%`,
        '--mouse-y': `${mousePosition.y}%`,
        '--tribe-primary': colors.primary,
        '--tribe-secondary': colors.secondary,
        '--tribe-glow': colors.glow,
        ...style
      }}
      onMouseMove={handleMouseMove}
      onMouseEnter={handleMouseEnter}
      onMouseLeave={handleMouseLeave}
      onClick={onClick}
      animate={{
        rotateX,
        rotateY,
        scale: isHovered ? 1.02 : 1,
      }}
      transition={{
        type: 'spring',
        stiffness: 300,
        damping: 20,
      }}
    >
      {/* Holographic Shine Layer */}
      <div className="holo-shine" />

      {/* Sparkle Overlay */}
      <div className="holo-sparkle" />

      {/* Rainbow Gradient */}
      <div className="holo-rainbow" />

      {/* Border Glow */}
      <div className="holo-border" />

      {/* Content */}
      <div className="holo-content">
        {children}
      </div>
    </motion.div>
  )
}

/**
 * Holographic Tribe Card - Pre-styled for Arcana tribes
 */
export function HoloTribeCard({ tribe, children, className = '' }) {
  return (
    <HolographicCard tribe={tribe.name} className={`holo-tribe-card ${className}`}>
      <div className="holo-tribe-inner">
        {children}
      </div>
    </HolographicCard>
  )
}
