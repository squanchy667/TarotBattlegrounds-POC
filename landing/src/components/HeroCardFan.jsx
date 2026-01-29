import { useRef, useEffect, useState } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import { gsap } from 'gsap'
import './HeroCardFan.css'

const HERO_CARDS = [
  { id: 5, rotation: -15, x: -120, delay: 0.1 },
  { id: 3, rotation: -5, x: -40, delay: 0.2 },
  { id: 4, rotation: 5, x: 40, delay: 0.3 },
  { id: 6, rotation: 15, x: 120, delay: 0.4 },
]

export default function HeroCardFan() {
  const containerRef = useRef(null)
  const cardsRef = useRef([])
  const [expandedCard, setExpandedCard] = useState(null)

  useEffect(() => {
    // Subtle floating animation for each card
    cardsRef.current.forEach((card, i) => {
      if (!card) return
      gsap.to(card, {
        y: -8 + (i % 2 === 0 ? 4 : -4),
        rotation: HERO_CARDS[i].rotation + (i % 2 === 0 ? 2 : -2),
        duration: 3 + i * 0.5,
        ease: 'sine.inOut',
        repeat: -1,
        yoyo: true,
        delay: i * 0.2,
      })
    })
  }, [])

  const handleCardClick = (cardId) => {
    setExpandedCard(cardId)
  }

  const handleClose = () => {
    setExpandedCard(null)
  }

  return (
    <>
      <div ref={containerRef} className="hero-card-fan">
        <div className="fan-glow" />
        {HERO_CARDS.map((card, index) => (
          <motion.div
            key={card.id}
            ref={(el) => (cardsRef.current[index] = el)}
            className="fan-card"
            initial={{
              opacity: 0,
              y: 100,
              rotate: card.rotation - 20,
              x: card.x
            }}
            animate={{
              opacity: 1,
              y: 0,
              rotate: card.rotation,
              x: card.x
            }}
            transition={{
              delay: 0.5 + card.delay,
              duration: 0.8,
              ease: [0.16, 1, 0.3, 1],
            }}
            style={{
              '--card-rotation': `${card.rotation}deg`,
              '--card-x': `${card.x}px`,
              zIndex: 10 - Math.abs(card.rotation),
            }}
            onClick={() => handleCardClick(card.id)}
          >
            <img
              src={`/images/cards/${card.id}.webp`}
              alt="Tarot card"
              className="fan-card-image"
              loading="eager"
              decoding="async"
              fetchPriority={index === 0 ? 'high' : 'auto'}
              width="826"
              height="1417"
            />
            <div className="fan-card-shine" />
          </motion.div>
        ))}
      </div>

      {/* Expanded Card Modal */}
      <AnimatePresence>
        {expandedCard && (
          <motion.div
            className="card-modal-overlay"
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            onClick={handleClose}
          >
            <motion.div
              className="card-modal"
              initial={{ scale: 0.5, opacity: 0 }}
              animate={{ scale: 1, opacity: 1 }}
              exit={{ scale: 0.5, opacity: 0 }}
              transition={{ type: 'spring', damping: 25, stiffness: 300 }}
              onClick={(e) => e.stopPropagation()}
            >
              <img
                src={`/images/cards/${expandedCard}.webp`}
                alt="Tarot card"
                className="card-modal-image"
                decoding="async"
                width="826"
                height="1417"
              />
              <button className="card-modal-close" onClick={handleClose}>
                ✕
              </button>
            </motion.div>
          </motion.div>
        )}
      </AnimatePresence>
    </>
  )
}
