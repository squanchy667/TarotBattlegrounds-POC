import { motion } from 'framer-motion'
import TribeCard from './TribeCard'

const TRIBES = [
  {
    name: 'Pentacles',
    symbol: '⬡',
    color: '#C0A060',
    theme: 'Wealth & Economy',
    desc: 'Generate gold, scale into the late game',
    bonus: '+1 Gold per round at 4+ Pentacles',
  },
  {
    name: 'Cups',
    symbol: '◈',
    color: '#4090FF',
    theme: 'Healing & Restoration',
    desc: 'Regenerate health, outlast your rivals',
    bonus: '+2 HP restored per combat at 4+ Cups',
  },
  {
    name: 'Swords',
    symbol: '⚔',
    color: '#A03030',
    theme: 'Aggression & Damage',
    desc: 'Strike fast, dominate early rounds',
    bonus: '+2 Attack to all at 4+ Swords',
  },
  {
    name: 'Wands',
    symbol: '✦',
    color: '#8B5CF6',
    theme: 'Magic & Buffs',
    desc: 'Amplify your board with stacking power',
    bonus: '+1/+1 to random ally per turn at 4+ Wands',
  },
]

export default function ArcanaSection({ isVisible }) {
  return (
    <section id="arcana" className={`section ${isVisible ? 'visible' : ''}`}>
      <motion.div
        className="section-header"
        initial={{ opacity: 0, y: 30 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={{ once: true }}
        transition={{ duration: 0.6 }}
      >
        <span className="section-tag">// THE_FOUR_ARCANA</span>
        <h2>Master the cosmic forces.</h2>
        <p className="section-desc">
          Each arcana represents a fundamental force. Combine them for devastating synergies.
        </p>
      </motion.div>

      <div className="tribes-grid">
        {TRIBES.map((tribe, i) => (
          <TribeCard key={tribe.name} tribe={tribe} index={i} />
        ))}
      </div>

      <motion.div
        className="tribes-tip"
        initial={{ opacity: 0, y: 20 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={{ once: true }}
        transition={{ delay: 0.5, duration: 0.6 }}
      >
        <span className="tip-icon">◈</span>
        <p>
          <strong>Pro tip:</strong> Cards can belong to multiple arcana. A Pentacles-Wands hybrid
          triggers both synergy bonuses!
        </p>
      </motion.div>
    </section>
  )
}
