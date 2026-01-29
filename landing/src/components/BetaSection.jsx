import { useState, useEffect, useRef } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import { gsap } from 'gsap'
import { animateCounter } from '../utils/animations'

export default function BetaSection({ isVisible, onCtaClick }) {
  const [email, setEmail] = useState('')
  const [platform, setPlatform] = useState('PC')
  const [submitted, setSubmitted] = useState(false)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const statsRef = useRef(null)
  const crystalLeftRef = useRef(null)
  const crystalRightRef = useRef(null)

  useEffect(() => {
    // Animate floating crystals
    if (crystalLeftRef.current) {
      gsap.to(crystalLeftRef.current, {
        y: -30,
        rotation: 10,
        duration: 6,
        ease: 'sine.inOut',
        repeat: -1,
        yoyo: true,
      })
    }

    if (crystalRightRef.current) {
      gsap.to(crystalRightRef.current, {
        y: -20,
        rotation: -8,
        duration: 7,
        ease: 'sine.inOut',
        repeat: -1,
        yoyo: true,
      })
    }
  }, [])

  // Animate counters when section becomes visible
  useEffect(() => {
    if (isVisible && statsRef.current) {
      const statNums = statsRef.current.querySelectorAll('.stat-num')
      if (statNums[0]) {
        animateCounter(statNums[0], 2847, { duration: 2 })
      }
    }
  }, [isVisible])

  const handleSubmit = (e) => {
    e.preventDefault()
    setIsSubmitting(true)
    onCtaClick?.('form_submit')

    // Submit to beehiiv via hidden iframe
    const form = document.createElement('form')
    form.method = 'POST'
    form.action = 'https://subscribe-forms.beehiiv.com/c8903279-67af-4d3a-b75a-2ad7b9c5da8b'
    form.target = 'beehiiv-iframe'

    const emailInput = document.createElement('input')
    emailInput.type = 'hidden'
    emailInput.name = 'email'
    emailInput.value = email
    form.appendChild(emailInput)

    document.body.appendChild(form)
    form.submit()
    document.body.removeChild(form)

    setTimeout(() => {
      setIsSubmitting(false)
      setSubmitted(true)
    }, 1000)
  }

  return (
    <section id="beta" className={`section section-glow ${isVisible ? 'visible' : ''}`}>
      <div className="beta-bg" aria-hidden="true" />

      <motion.img
        ref={crystalLeftRef}
        src="/images/crystal-gem-2.webp"
        alt=""
        className="beta-crystal beta-crystal-left"
        aria-hidden="true"
        loading="lazy"
        decoding="async"
        width="2816"
        height="1536"
        initial={{ opacity: 0, x: -50 }}
        whileInView={{ opacity: 0.5, x: 0 }}
        viewport={{ once: true }}
        transition={{ duration: 1 }}
      />

      <motion.img
        ref={crystalRightRef}
        src="/images/crystal-gem-3.webp"
        alt=""
        className="beta-crystal beta-crystal-right"
        aria-hidden="true"
        loading="lazy"
        decoding="async"
        width="2816"
        height="1536"
        initial={{ opacity: 0, x: 50 }}
        whileInView={{ opacity: 0.5, x: 0 }}
        viewport={{ once: true }}
        transition={{ duration: 1 }}
      />

      <motion.div
        className="section-header"
        initial={{ opacity: 0, y: 30 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={{ once: true }}
        transition={{ duration: 0.6 }}
      >
        <span className="section-tag">// INITIALIZE_SEQUENCE</span>
        <h2>Step into the first circle of Arcanists</h2>
        <p className="section-desc">Shape the arena with your feedback. Early access awaits.</p>
      </motion.div>

      <div className="beta-content">
        <AnimatePresence mode="wait">
          {!submitted ? (
            <motion.form
              key="form"
              className="beta-form"
              onSubmit={handleSubmit}
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: -20, scale: 0.95 }}
              transition={{ duration: 0.5 }}
            >
              <div className="form-fields">
                <label className="form-field">
                  <span className="field-label">// EMAIL_ADDRESS</span>
                  <motion.input
                    type="email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    placeholder="arcanist@domain.com"
                    required
                    whileFocus={{ borderColor: '#C0A060', boxShadow: '0 0 20px rgba(192, 160, 96, 0.3)' }}
                  />
                </label>
                <label className="form-field">
                  <span className="field-label">// PLATFORM</span>
                  <select value={platform} onChange={(e) => setPlatform(e.target.value)}>
                    <option value="PC">PC / Mac</option>
                    <option value="iOS">iOS</option>
                    <option value="Android">Android</option>
                  </select>
                </label>
              </div>
              <motion.button
                className="btn btn-primary btn-lg"
                type="submit"
                disabled={isSubmitting}
                whileHover={{ scale: 1.02, boxShadow: '0 0 50px rgba(192, 160, 96, 0.5)' }}
                whileTap={{ scale: 0.98 }}
              >
                <span>{isSubmitting ? 'Initializing...' : 'Become a beta tester'}</span>
                <span className="btn-shine" />
              </motion.button>
              <p className="form-note">We respect your data. Unsubscribe anytime.</p>
            </motion.form>
          ) : (
            <motion.div
              key="success"
              className="beta-success"
              initial={{ opacity: 0, scale: 0.9 }}
              animate={{ opacity: 1, scale: 1 }}
              transition={{ duration: 0.5, ease: [0.16, 1, 0.3, 1] }}
            >
              <motion.div
                className="success-icon"
                initial={{ scale: 0 }}
                animate={{ scale: 1 }}
                transition={{ delay: 0.2, type: 'spring', stiffness: 200 }}
              >
                ✓
              </motion.div>
              <h3>Sequence Initialized</h3>
              <p>Welcome, Arcanist. You'll receive word when the Circle opens.</p>
              <motion.div
                className="success-particles"
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                transition={{ delay: 0.5 }}
              />
            </motion.div>
          )}
        </AnimatePresence>

        <motion.div
          ref={statsRef}
          className="beta-stats"
          initial={{ opacity: 0, y: 30 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true }}
          transition={{ delay: 0.3, duration: 0.6 }}
        >
          <div className="beta-stat">
            <span className="stat-num">0</span>
            <span className="stat-label">Arcanists Enlisted</span>
          </div>
          <div className="beta-stat">
            <span className="stat-num">Q2 2026</span>
            <span className="stat-label">Beta Launch</span>
          </div>
          <div className="beta-stat">
            <span className="stat-num">50+</span>
            <span className="stat-label">Cards at Launch</span>
          </div>
        </motion.div>
      </div>
    </section>
  )
}
