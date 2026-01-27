import { motion } from 'framer-motion'

export default function Footer({ onScrollTo, onCtaClick }) {
  return (
    <footer className="footer">
      <motion.div
        className="footer-cta"
        initial={{ opacity: 0, y: 30 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={{ once: true }}
        transition={{ duration: 0.6 }}
      >
        <h2>The rite begins soon.</h2>
        <p>Join the beta and help shape the arena.</p>
        <motion.button
          className="btn btn-primary"
          onClick={() => {
            onCtaClick?.('footer_beta')
            onScrollTo('beta')
          }}
          whileHover={{ scale: 1.05, boxShadow: '0 0 50px rgba(192, 160, 96, 0.5)' }}
          whileTap={{ scale: 0.95 }}
        >
          <span>Initialize Beta Sequence</span>
          <span className="btn-shine" />
        </motion.button>
      </motion.div>

      <div className="footer-bottom">
        <motion.div
          className="footer-brand"
          initial={{ opacity: 0 }}
          whileInView={{ opacity: 1 }}
          viewport={{ once: true }}
          transition={{ delay: 0.2 }}
        >
          <img src="/images/logo.png" alt="" className="footer-logo" />
          <span>Tarot: Arcana Circle</span>
        </motion.div>

        <motion.div
          className="footer-links"
          initial={{ opacity: 0 }}
          whileInView={{ opacity: 1 }}
          viewport={{ once: true }}
          transition={{ delay: 0.3 }}
        >
          <button onClick={() => onScrollTo('gameplay')}>Gameplay</button>
          <button onClick={() => onScrollTo('arcana')}>Arcana</button>
          <button onClick={() => onScrollTo('beta')}>Beta</button>
        </motion.div>

        <motion.p
          className="footer-copy"
          initial={{ opacity: 0 }}
          whileInView={{ opacity: 1 }}
          viewport={{ once: true }}
          transition={{ delay: 0.4 }}
        >
          © 2026 Tarot: Arcana Circle. Your fate awaits.
        </motion.p>
      </div>

      {/* Decorative gear */}
      <div className="footer-gear" aria-hidden="true">
        <svg viewBox="0 0 100 100" className="gear-svg">
          <circle cx="50" cy="50" r="30" fill="none" stroke="rgba(192, 160, 96, 0.1)" strokeWidth="2" />
          <circle cx="50" cy="50" r="20" fill="none" stroke="rgba(192, 160, 96, 0.05)" strokeWidth="1" />
        </svg>
      </div>
    </footer>
  )
}
