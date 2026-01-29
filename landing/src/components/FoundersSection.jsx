import { motion } from 'framer-motion'

export default function FoundersSection({ isVisible }) {
  return (
    <section id="founders" className={`section ${isVisible ? 'visible' : ''}`}>
      <motion.div
        className="founders-card"
        initial={{ opacity: 0, y: 30 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={{ once: true }}
        transition={{ duration: 0.7 }}
      >
        <div className="founders-quote">
          <motion.p
            initial={{ opacity: 0 }}
            whileInView={{ opacity: 1 }}
            viewport={{ once: true }}
            transition={{ delay: 0.3, duration: 0.8 }}
          >
            We're two brothers who grew up obsessively playing trading card games and strategy games of all kinds.
          </motion.p>
          <motion.p
            initial={{ opacity: 0 }}
            whileInView={{ opacity: 1 }}
            viewport={{ once: true }}
            transition={{ delay: 0.4, duration: 0.8 }}
          >
            Over time, we realized something: the best part of TCGs isn't collecting or grinding—it's the execution of strategy, the tension, and the thrill of a clever play.
          </motion.p>
          <motion.p
            initial={{ opacity: 0 }}
            whileInView={{ opacity: 1 }}
            viewport={{ once: true }}
            transition={{ delay: 0.5, duration: 0.8 }}
          >
            We wanted to create that same feeling without the barriers. No endless packs or deck building homework. Just deep strategy, fair competition, and instant play.
          </motion.p>
          <motion.p
            initial={{ opacity: 0 }}
            whileInView={{ opacity: 1 }}
            viewport={{ once: true }}
            transition={{ delay: 0.6, duration: 0.8 }}
            className="founders-closing"
          >
            We think we've found it, and we're excited to build it with you.
          </motion.p>
        </div>
        <motion.div
          className="founders-sig"
          initial={{ opacity: 0, x: -20 }}
          whileInView={{ opacity: 1, x: 0 }}
          viewport={{ once: true }}
          transition={{ delay: 0.7, duration: 0.6 }}
        >
          <span className="sig-line" />
          <span className="sig-name">— The Founders</span>
        </motion.div>
      </motion.div>
    </section>
  )
}
