import { useEffect, useRef } from 'react'
import { motion } from 'framer-motion'
import { gsap } from 'gsap'

const titleVariants = {
  hidden: { opacity: 0 },
  visible: {
    opacity: 1,
    transition: {
      staggerChildren: 0.08,
    },
  },
}

const wordVariants = {
  hidden: { opacity: 0, y: 50, rotateX: -90 },
  visible: {
    opacity: 1,
    y: 0,
    rotateX: 0,
    transition: {
      duration: 0.6,
      ease: [0.16, 1, 0.3, 1],
    },
  },
}

const chipVariants = {
  hidden: { opacity: 0, scale: 0.8 },
  visible: (i) => ({
    opacity: 1,
    scale: 1,
    transition: {
      delay: 0.8 + i * 0.1,
      duration: 0.4,
      ease: [0.16, 1, 0.3, 1],
    },
  }),
}

export default function HeroSection({ onScrollTo, onCtaClick }) {
  const heroImageRef = useRef(null)
  const glowRef = useRef(null)

  useEffect(() => {
    if (heroImageRef.current) {
      gsap.to(heroImageRef.current, {
        y: -15,
        duration: 4,
        ease: 'sine.inOut',
        repeat: -1,
        yoyo: true,
      })
    }

    if (glowRef.current) {
      gsap.to(glowRef.current, {
        scale: 1.15,
        opacity: 0.6,
        duration: 3,
        ease: 'sine.inOut',
        repeat: -1,
        yoyo: true,
      })
    }
  }, [])

  const titleWords1 = 'Read the cards.'.split(' ')
  const titleWords2 = 'Outsmart the table.'.split(' ')

  return (
    <header className="hero">
      <div className="hero-content">
        <motion.div
          className="hero-text"
          initial="hidden"
          animate="visible"
          variants={{
            hidden: {},
            visible: { transition: { staggerChildren: 0.1 } },
          }}
        >
          <motion.p
            className="eyebrow"
            initial={{ opacity: 0, x: -30 }}
            animate={{ opacity: 1, x: 0 }}
            transition={{ duration: 0.6, delay: 0.2 }}
          >
            <span className="eyebrow-icon">◇</span>
            The Arcana Circle opens
          </motion.p>

          <motion.h1 className="hero-title" variants={titleVariants}>
            <span className="title-line">
              {titleWords1.map((word, i) => (
                <motion.span key={i} className="word-wrapper" variants={wordVariants}>
                  {word}{' '}
                </motion.span>
              ))}
            </span>
            <span className="title-accent">
              {titleWords2.map((word, i) => (
                <motion.span key={i} className="word-wrapper" variants={wordVariants}>
                  {word}{' '}
                </motion.span>
              ))}
            </span>
          </motion.h1>

          <motion.p
            className="hero-sub"
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6, delay: 0.6 }}
          >
            An epic multiplayer arena where insight and wit win the game. Draft from rotating
            spreads, forge synergies, and let fate collide.
          </motion.p>

          <div className="hero-chips">
            {[
              { icon: '⟳', text: 'Auto-Combat' },
              { icon: '◈', text: '4 Arcana' },
              { icon: '⚡', text: '20-min matches' },
            ].map((chip, i) => (
              <motion.span
                key={chip.text}
                className="chip"
                custom={i}
                initial="hidden"
                animate="visible"
                variants={chipVariants}
              >
                <span className="chip-icon">{chip.icon}</span> {chip.text}
              </motion.span>
            ))}
          </div>

          <motion.div
            className="hero-cta"
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6, delay: 1 }}
          >
            <motion.button
              className="btn btn-primary btn-lg"
              onClick={() => {
                onCtaClick('hero_beta')
                onScrollTo('beta')
              }}
              whileHover={{ scale: 1.05, boxShadow: '0 0 60px rgba(192, 160, 96, 0.5)' }}
              whileTap={{ scale: 0.98 }}
            >
              <span>Initialize Beta Sequence</span>
              <span className="btn-shine" />
            </motion.button>
            <motion.button
              className="btn btn-ghost"
              onClick={() => onScrollTo('gameplay')}
              whileHover={{ scale: 1.02 }}
              whileTap={{ scale: 0.98 }}
            >
              How It Works
            </motion.button>
          </motion.div>

          <motion.div
            className="hero-features"
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{ duration: 0.8, delay: 1.2 }}
          >
            {[
              'Arcanists duel in rotating card spreads',
              'Every round sharpens your instincts',
              'Arcane tactics, crystal-clear outcomes',
            ].map((feature, i) => (
              <motion.div
                key={i}
                className="feature-line"
                initial={{ opacity: 0, x: -20 }}
                animate={{ opacity: 1, x: 0 }}
                transition={{ delay: 1.3 + i * 0.1 }}
              >
                <span className="line-marker" />
                {feature}
              </motion.div>
            ))}
          </motion.div>
        </motion.div>

        <motion.div
          className="hero-visual"
          initial={{ opacity: 0, scale: 0.9, x: 50 }}
          animate={{ opacity: 1, scale: 1, x: 0 }}
          transition={{ duration: 0.8, delay: 0.3, ease: [0.16, 1, 0.3, 1] }}
        >
          <div className="hero-image-wrapper">
            <img
              ref={heroImageRef}
              src="/images/hero-battle.png"
              alt="Epic battle in the Arcana Circle"
              className="hero-image"
            />
            <div ref={glowRef} className="hero-image-glow" />
          </div>
        </motion.div>
      </div>

      <motion.div
        className="scroll-hint"
        onClick={() => onScrollTo('gameplay')}
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 0.6, y: 0 }}
        transition={{ delay: 1.5, duration: 0.6 }}
        whileHover={{ opacity: 1 }}
      >
        <span className="scroll-text">Scroll to explore</span>
        <motion.span
          className="scroll-arrow"
          animate={{ y: [0, 8, 0] }}
          transition={{ duration: 1.5, repeat: Infinity, ease: 'easeInOut' }}
        >
          ↓
        </motion.span>
      </motion.div>
    </header>
  )
}
