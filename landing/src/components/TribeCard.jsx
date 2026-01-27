import { motion } from 'framer-motion'
import HolographicCard from './HolographicCard'
import './HolographicCard.css'

export default function TribeCard({ tribe, index }) {
  const tribeImageMap = {
    Pentacles: '/images/tribe-pentacles.png',
    Cups: '/images/tribe-cups.png',
    Swords: '/images/tribe-swords.png',
    Wands: '/images/tribe-wands.png',
  }

  return (
    <HolographicCard
      tribe={tribe.name}
      className="tribe-card-holo"
      style={{ '--tribe-color': tribe.color }}
    >
      <motion.div
        className="tribe-card-content"
        initial={{ opacity: 0, y: 30 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={{ once: true, margin: '-50px' }}
        transition={{
          delay: index * 0.1,
          duration: 0.6,
          ease: [0.16, 1, 0.3, 1],
        }}
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
            />
          </motion.div>

        <h3>{tribe.name}</h3>
        <span className="tribe-theme">{tribe.theme}</span>
        <p>{tribe.desc}</p>

        <div className="tribe-bonus">
          <span className="bonus-label">// SYNERGY_BONUS</span>
          <span className="bonus-text">{tribe.bonus}</span>
        </div>
      </motion.div>
    </HolographicCard>
  )
}
