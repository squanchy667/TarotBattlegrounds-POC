import { useState, useRef } from 'react'
import { motion } from 'framer-motion'
import HolographicCard from './HolographicCard'
import './HolographicCard.css'

const TRIBE_EXAMPLE_CARDS = {
  Pentacles: { id: 3, name: 'Torch Bearer' },
  Swords: { id: 4, name: 'Solar Warrior' },
  Cups: { id: 5, name: 'Mystical Swan' },
  Wands: { id: 6, name: 'Dragon of Creation' },
}

export default function TribeCard({ tribe, index }) {
  const [showCard, setShowCard] = useState(false)
  const cardRef = useRef(null)

  const tribeImageMap = {
    Pentacles: '/images/tribe-pentacles.webp',
    Cups: '/images/tribe-cups.webp',
    Swords: '/images/tribe-swords.webp',
    Wands: '/images/tribe-wands.webp',
  }

  const exampleCard = TRIBE_EXAMPLE_CARDS[tribe.name]

  return (
    <HolographicCard
      tribe={tribe.name}
      className="tribe-card-holo"
      style={{ '--tribe-color': tribe.color }}
    >
      <motion.div
        ref={cardRef}
        className="tribe-card-content"
        initial={{ opacity: 0, y: 30 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={{ once: true, margin: '-50px' }}
        transition={{
          delay: index * 0.1,
          duration: 0.6,
          ease: [0.16, 1, 0.3, 1],
        }}
        onMouseEnter={() => setShowCard(true)}
        onMouseLeave={() => setShowCard(false)}
      >
        <motion.div
          className="tribe-symbol-wrapper"
          whileHover={{ scale: 1.15, rotate: 10 }}
          transition={{ type: 'spring', stiffness: 300 }}
        >
          <img
            src={tribeImageMap[tribe.name]}
            alt={`${tribe.name} emblem`}
            className="tribe-symbol-img"
            loading="lazy"
            decoding="async"
            width="512"
            height="512"
          />
        </motion.div>

        <h3>{tribe.name}</h3>
        <span className="tribe-theme">{tribe.theme}</span>
        <p>{tribe.desc}</p>

        <div className="tribe-bonus">
          <span className="bonus-label">// SYNERGY_BONUS</span>
          <span className="bonus-text">{tribe.bonus}</span>
        </div>

        {exampleCard && (
          <motion.div
            className="tribe-example-card"
            initial={{ opacity: 0, scale: 0.8, y: 20 }}
            animate={{
              opacity: showCard ? 1 : 0,
              scale: showCard ? 1 : 0.8,
              y: showCard ? 0 : 20,
            }}
            transition={{ duration: 0.3 }}
          >
            <img
              src={`/images/cards/${exampleCard.id}.webp`}
              alt={exampleCard.name}
              className="tribe-example-card-img"
              loading="lazy"
              decoding="async"
              width="826"
              height="1417"
            />
          </motion.div>
        )}
      </motion.div>
    </HolographicCard>
  )
}
