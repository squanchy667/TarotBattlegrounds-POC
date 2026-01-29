import { useRef, useEffect } from 'react'
import { motion } from 'framer-motion'
import { gsap } from 'gsap'
import { ScrollTrigger } from 'gsap/ScrollTrigger'

gsap.registerPlugin(ScrollTrigger)

const steps = [
  {
    num: '01',
    icon: '/images/pick.webp',
    iconWidth: 524,
    iconHeight: 524,
    title: 'Pick Your Cards',
    desc: 'Draft from a shared pool of rotating cards. Spend gold to build your army.',
    statLabel: 'Duration',
    statValue: '35 sec',
  },
  {
    num: '02',
    icon: '/images/place.webp',
    iconWidth: 528,
    iconHeight: 520,
    title: 'Place Your Units',
    desc: 'Position up to 7 cards on your board. Matching tribes unlock powerful bonuses.',
    statLabel: 'Board Size',
    statValue: '7 cards',
  },
  {
    num: '03',
    icon: '/images/watch.webp',
    iconWidth: 542,
    iconHeight: 522,
    title: 'Watch Them Battle',
    desc: 'Combat is automatic. Your strategy plays out—no clicking required.',
    statLabel: 'Combat',
    statValue: 'Auto',
  },
]

const cardVariants = {
  hidden: { opacity: 0, y: 60, rotateX: -15 },
  visible: (i) => ({
    opacity: 1,
    y: 0,
    rotateX: 0,
    transition: {
      delay: i * 0.15,
      duration: 0.7,
      ease: [0.16, 1, 0.3, 1],
    },
  }),
}

export default function GameplaySection({ isVisible }) {
  const loopRef = useRef(null)
  const connectorsRef = useRef(null)

  useEffect(() => {
    if (!loopRef.current) return

    // Animate loop ring rotation
    gsap.to(loopRef.current.querySelector('.loop-ring'), {
      rotation: 360,
      duration: 30,
      ease: 'none',
      repeat: -1,
    })

    // Animate connecting lines
    if (connectorsRef.current) {
      const lines = connectorsRef.current.querySelectorAll('.connector-line')
      gsap.fromTo(
        lines,
        { strokeDashoffset: 100 },
        {
          strokeDashoffset: 0,
          duration: 1.5,
          stagger: 0.3,
          ease: 'power2.out',
          scrollTrigger: {
            trigger: connectorsRef.current,
            start: 'top 80%',
            toggleActions: 'play none none none',
          },
        }
      )
    }
  }, [])

  return (
    <section id="gameplay" className={`section ${isVisible ? 'visible' : ''}`}>
      <motion.div
        className="section-header"
        initial={{ opacity: 0, y: 30 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={{ once: true }}
        transition={{ duration: 0.6 }}
      >
        <span className="section-tag">// HOW_IT_WORKS</span>
        <h2>8 Players, Endless Possibilities</h2>
        <p className="section-desc">Every round has 3 simple phases: Pick, Place, Battle. Repeat until one player remains.</p>
      </motion.div>

      {/* Connecting Lines SVG */}
      <svg
        ref={connectorsRef}
        className="steps-connectors"
        viewBox="0 0 1200 100"
        preserveAspectRatio="none"
      >
        <defs>
          <linearGradient id="lineGradient" x1="0%" y1="0%" x2="100%" y2="0%">
            <stop offset="0%" stopColor="#4090FF" stopOpacity="0" />
            <stop offset="50%" stopColor="#4090FF" stopOpacity="0.5" />
            <stop offset="100%" stopColor="#4090FF" stopOpacity="0" />
          </linearGradient>
        </defs>
        <path
          className="connector-line"
          d="M 200 50 L 500 50"
          stroke="url(#lineGradient)"
          strokeWidth="2"
          strokeDasharray="100"
          fill="none"
        />
        <path
          className="connector-line"
          d="M 700 50 L 1000 50"
          stroke="url(#lineGradient)"
          strokeWidth="2"
          strokeDasharray="100"
          fill="none"
        />
      </svg>

      <div className="steps-grid">
        {steps.map((step, i) => (
          <motion.article
            key={step.num}
            className="step-card"
            custom={i}
            initial="hidden"
            whileInView="visible"
            viewport={{ once: true, margin: '-50px' }}
            variants={cardVariants}
          >
            <div className="step-num">{step.num}</div>
            <motion.div
              className="step-icon"
              whileHover={{ scale: 1.1 }}
              transition={{ type: 'spring', stiffness: 300 }}
            >
              <img
                src={step.icon}
                alt={step.title}
                className="step-icon-img"
                loading="lazy"
                decoding="async"
                width={step.iconWidth}
                height={step.iconHeight}
              />
            </motion.div>
            <h3>{step.title}</h3>
            <p>{step.desc}</p>
            <div className="step-stat">
              <span className="stat-label">{step.statLabel}</span>
              <span className="stat-value">{step.statValue}</span>
            </div>
          </motion.article>
        ))}
      </div>

      <motion.div
        ref={loopRef}
        className="loop-diagram"
        initial={{ opacity: 0, scale: 0.8 }}
        whileInView={{ opacity: 1, scale: 1 }}
        viewport={{ once: true }}
        transition={{ duration: 0.8, delay: 0.5 }}
      >
        <div className="loop-ring" />
        <div className="loop-inner-ring" />
        <div className="loop-center">
          <span className="loop-icon">∞</span>
          <span>Until 1 remains</span>
        </div>
        <div className="loop-nodes">
          <motion.span
            className="loop-node"
            style={{ '--angle': '-30deg' }}
            animate={{ scale: [1, 1.05, 1] }}
            transition={{ duration: 2, repeat: Infinity, delay: 0 }}
          >
            Pick
          </motion.span>
          <motion.span
            className="loop-node"
            style={{ '--angle': '90deg' }}
            animate={{ scale: [1, 1.05, 1] }}
            transition={{ duration: 2, repeat: Infinity, delay: 0.6 }}
          >
            Place
          </motion.span>
          <motion.span
            className="loop-node"
            style={{ '--angle': '210deg' }}
            animate={{ scale: [1, 1.05, 1] }}
            transition={{ duration: 2, repeat: Infinity, delay: 1.2 }}
          >
            Battle
          </motion.span>
        </div>
      </motion.div>
    </section>
  )
}
