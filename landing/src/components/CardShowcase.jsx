import { useState, useRef } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import './CardShowcase.css'

const CARDS = [
  { id: 3, name: 'Torch Bearer', tribe: 'Pentacles', stats: '2/2', ability: 'Your other units have +1/+1' },
  { id: 4, name: 'Solar Warrior', tribe: 'Swords', stats: '2/6', ability: 'When a friendly unit dies, give your other units +2 attack permanently' },
  { id: 5, name: 'Mystical Swan', tribe: 'Cups', stats: '2/5', ability: 'When a friendly unit takes damage, give other units +2 health' },
  { id: 6, name: 'Dragon of Creation', tribe: 'Wands', stats: '6/9', ability: 'For each Wand that died last combat, give your Wands +3/+3 permanently' },
  { id: 7, name: 'Card 7', tribe: 'Mixed', stats: '?/?', ability: 'Discover this card in the beta' },
  { id: 8, name: 'Card 8', tribe: 'Mixed', stats: '?/?', ability: 'Discover this card in the beta' },
]

const TRIBE_COLORS = {
  Pentacles: '#C0A060',
  Swords: '#A03030',
  Cups: '#4090FF',
  Wands: '#8B5CF6',
  Mixed: '#C0A060',
}

export default function CardShowcase({ isVisible }) {
  const [activeCard, setActiveCard] = useState(null)
  const [mousePosition, setMousePosition] = useState({ x: 50, y: 50 })

  const handleMouseMove = (e, cardRef) => {
    if (!cardRef) return
    const rect = cardRef.getBoundingClientRect()
    const x = ((e.clientX - rect.left) / rect.width) * 100
    const y = ((e.clientY - rect.top) / rect.height) * 100
    setMousePosition({ x, y })
  }

  return (
    <section id="cards" className={`section card-showcase-section ${isVisible ? 'visible' : ''}`}>
      <motion.div
        className="section-header"
        initial={{ opacity: 0, y: 30 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={{ once: true }}
        transition={{ duration: 0.6 }}
      >
        <span className="section-tag">// CARD_PREVIEW</span>
        <h2>Behold the Arcana.</h2>
        <p className="section-desc">
          Each card is a work of art. Hover to reveal their power.
        </p>
      </motion.div>

      <div className="cards-carousel">
        {CARDS.slice(0, 4).map((card, index) => (
          <ShowcaseCard
            key={card.id}
            card={card}
            index={index}
            isActive={activeCard === card.id}
            onHover={() => setActiveCard(card.id)}
            onLeave={() => setActiveCard(null)}
          />
        ))}
      </div>

      <motion.div
        className="showcase-cta"
        initial={{ opacity: 0, y: 20 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={{ once: true }}
        transition={{ delay: 0.4, duration: 0.6 }}
      >
        <p className="showcase-note">
          <span className="note-icon">◈</span>
          50+ unique cards at launch, each with distinct abilities and synergies
        </p>
      </motion.div>
    </section>
  )
}

function ShowcaseCard({ card, index, isActive, onHover, onLeave }) {
  const cardRef = useRef(null)
  const [tilt, setTilt] = useState({ x: 0, y: 0 })
  const [glarePosition, setGlarePosition] = useState({ x: 50, y: 50 })

  const handleMouseMove = (e) => {
    if (!cardRef.current) return
    const rect = cardRef.current.getBoundingClientRect()
    const x = (e.clientX - rect.left) / rect.width
    const y = (e.clientY - rect.top) / rect.height

    setTilt({
      x: (y - 0.5) * 20,
      y: (x - 0.5) * -20,
    })
    setGlarePosition({
      x: x * 100,
      y: y * 100,
    })
  }

  const handleMouseLeave = () => {
    setTilt({ x: 0, y: 0 })
    setGlarePosition({ x: 50, y: 50 })
    onLeave()
  }

  const tribeColor = TRIBE_COLORS[card.tribe] || '#C0A060'

  return (
    <motion.div
      ref={cardRef}
      className={`showcase-card ${isActive ? 'active' : ''}`}
      initial={{ opacity: 0, y: 50, rotateY: -15 }}
      whileInView={{ opacity: 1, y: 0, rotateY: 0 }}
      viewport={{ once: true }}
      transition={{ delay: index * 0.1, duration: 0.6, ease: [0.16, 1, 0.3, 1] }}
      onMouseMove={handleMouseMove}
      onMouseEnter={onHover}
      onMouseLeave={handleMouseLeave}
      style={{
        '--tribe-color': tribeColor,
        '--tilt-x': `${tilt.x}deg`,
        '--tilt-y': `${tilt.y}deg`,
        '--glare-x': `${glarePosition.x}%`,
        '--glare-y': `${glarePosition.y}%`,
      }}
    >
      <div className="card-inner">
        <img
          src={`/images/cards/${card.id}.webp`}
          alt={card.name}
          className="card-image"
          loading="lazy"
          decoding="async"
          width="826"
          height="1417"
        />
        <div className="card-glare" />
        <div className="card-shine" />
      </div>

      <AnimatePresence>
        {isActive && (
          <motion.div
            className="card-tooltip"
            initial={{ opacity: 0, y: 10, scale: 0.95 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: 10, scale: 0.95 }}
            transition={{ duration: 0.2 }}
          >
            <span className="tooltip-tribe" style={{ color: tribeColor }}>
              {card.tribe}
            </span>
            <span className="tooltip-name">{card.name}</span>
            <span className="tooltip-stats">{card.stats}</span>
          </motion.div>
        )}
      </AnimatePresence>
    </motion.div>
  )
}
