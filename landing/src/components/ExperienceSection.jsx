import { useEffect, useRef } from 'react'
import { motion } from 'framer-motion'
import { gsap } from 'gsap'

const experiences = [
  {
    icon: '⚙',
    title: 'Clarity in every round',
    desc: 'Every round sharpens your instincts and rewards smart choices. No hidden dice—just transparent strategy.',
  },
  {
    icon: '☾',
    title: 'Mysticism with intention',
    desc: 'Arcana archetypes guide your strategy with clear, dramatic outcomes. Magic you can understand.',
  },
  {
    icon: '◈',
    title: 'Tactical identity',
    desc: 'Shape a playstyle that feels distinctly yours. Aggressive, defensive, or synergy-focused—you decide.',
  },
]

const cardVariants = {
  hidden: { opacity: 0, y: 40 },
  visible: (i) => ({
    opacity: 1,
    y: 0,
    transition: {
      delay: 0.2 + i * 0.15,
      duration: 0.6,
      ease: [0.16, 1, 0.3, 1],
    },
  }),
}

export default function ExperienceSection({ isVisible }) {
  const crystalRef = useRef(null)

  useEffect(() => {
    if (crystalRef.current) {
      gsap.to(crystalRef.current, {
        y: -25,
        rotation: 8,
        duration: 5,
        ease: 'sine.inOut',
        repeat: -1,
        yoyo: true,
      })
    }
  }, [])

  return (
    <section id="experience" className={`section section-dark ${isVisible ? 'visible' : ''}`}>
      <motion.div
        className="experience-visual"
        initial={{ opacity: 0, x: 100 }}
        whileInView={{ opacity: 0.7, x: 0 }}
        viewport={{ once: true }}
        transition={{ duration: 1 }}
      >
        <img
          ref={crystalRef}
          src="/images/crystal-gem.png"
          alt=""
          className="crystal-float"
          aria-hidden="true"
        />
      </motion.div>

      <motion.div
        className="section-header"
        initial={{ opacity: 0, y: 30 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={{ once: true }}
        transition={{ duration: 0.6 }}
      >
        <span className="section-tag">// THE_EXPERIENCE</span>
        <h2>Simple to start. Satisfying to master.</h2>
      </motion.div>

      <div className="experience-grid">
        {experiences.map((exp, i) => (
          <motion.article
            key={exp.title}
            className="exp-card"
            custom={i}
            initial="hidden"
            whileInView="visible"
            viewport={{ once: true, margin: '-30px' }}
            variants={cardVariants}
            whileHover={{
              y: -6,
              borderColor: 'rgba(192, 160, 96, 0.5)',
              boxShadow: '0 20px 40px rgba(0, 0, 0, 0.3)',
            }}
          >
            <motion.div
              className="exp-icon"
              whileHover={{ scale: 1.2, rotate: 15 }}
              transition={{ type: 'spring', stiffness: 300 }}
            >
              {exp.icon}
            </motion.div>
            <h3>{exp.title}</h3>
            <p>{exp.desc}</p>
            <div className="exp-card-glow" />
          </motion.article>
        ))}
      </div>
    </section>
  )
}
