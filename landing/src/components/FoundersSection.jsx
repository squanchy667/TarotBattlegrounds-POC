import { motion } from 'framer-motion'

export default function FoundersSection() {
  return (
    <section id="founders" className="section">
      <motion.div
        className="founders-card"
        initial={{ opacity: 0, y: 30 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={{ once: true }}
        transition={{ duration: 0.7 }}
      >
        <div className="founders-quote">
          <span className="quote-mark">"</span>
          <motion.p
            initial={{ opacity: 0 }}
            whileInView={{ opacity: 1 }}
            viewport={{ once: true }}
            transition={{ delay: 0.3, duration: 0.8 }}
          >
            Tarot: Arcana Circle began as a love letter to strategy—fast, readable, and deeply
            rewarding. We believe a game can feel mystical and still be crystal-clear, competitive
            and still inviting. The beta will shape the arcana, the pacing, and the soul of the
            arena. We'd be honored to have you at the table.
          </motion.p>
        </div>
        <motion.div
          className="founders-sig"
          initial={{ opacity: 0, x: -20 }}
          whileInView={{ opacity: 1, x: 0 }}
          viewport={{ once: true }}
          transition={{ delay: 0.5, duration: 0.6 }}
        >
          <span className="sig-line" />
          <span className="sig-name">— The Tarot: Arcana Circle Team</span>
        </motion.div>
      </motion.div>
    </section>
  )
}
