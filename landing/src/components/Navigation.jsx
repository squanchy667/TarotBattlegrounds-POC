import { useState, useEffect } from 'react'
import { motion, AnimatePresence } from 'framer-motion'

export default function Navigation({ onScrollTo, onCtaClick }) {
  const [isScrolled, setIsScrolled] = useState(false)
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false)

  useEffect(() => {
    const handleScroll = () => {
      setIsScrolled(window.scrollY > 50)
    }
    window.addEventListener('scroll', handleScroll, { passive: true })
    return () => window.removeEventListener('scroll', handleScroll)
  }, [])

  const handleNavClick = (section) => {
    onScrollTo(section)
    setIsMobileMenuOpen(false)
  }

  return (
    <motion.nav
      className={`nav ${isScrolled ? 'nav-scrolled' : ''}`}
      initial={{ y: -100 }}
      animate={{ y: 0 }}
      transition={{ duration: 0.6, ease: [0.16, 1, 0.3, 1] }}
    >
      <div className="nav-brand">
        <motion.img
          src="/images/logo.png"
          alt="Tarot: Arcana Circle"
          className="brand-logo"
          whileHover={{ scale: 1.05, rotate: 5 }}
          transition={{ type: 'spring', stiffness: 300 }}
        />
        <div className="brand-text">
          <span className="brand-name">Tarot: Arcana Circle</span>
          <span className="brand-tag">Beta</span>
        </div>
      </div>

      {/* Desktop Navigation */}
      <div className="nav-links">
        <motion.button
          className="nav-link"
          onClick={() => handleNavClick('gameplay')}
          whileHover={{ color: '#60a8ff' }}
          transition={{ duration: 0.2 }}
        >
          Gameplay
        </motion.button>
        <motion.button
          className="nav-link"
          onClick={() => handleNavClick('arcana')}
          whileHover={{ color: '#60a8ff' }}
          transition={{ duration: 0.2 }}
        >
          The Arcana
        </motion.button>
        <motion.button
          className="btn btn-nav"
          onClick={() => {
            onCtaClick('nav_beta')
            handleNavClick('beta')
          }}
          whileHover={{ scale: 1.05 }}
          whileTap={{ scale: 0.95 }}
        >
          Initialize Beta
        </motion.button>
      </div>

      {/* Mobile Menu Button */}
      <button
        className="nav-mobile-toggle"
        onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
        aria-label="Toggle menu"
      >
        <span className={`hamburger ${isMobileMenuOpen ? 'open' : ''}`}>
          <span></span>
          <span></span>
          <span></span>
        </span>
      </button>

      {/* Mobile Menu */}
      <AnimatePresence>
        {isMobileMenuOpen && (
          <motion.div
            className="nav-mobile-menu"
            initial={{ opacity: 0, y: -20 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -20 }}
            transition={{ duration: 0.3 }}
          >
            <button onClick={() => handleNavClick('gameplay')}>Gameplay</button>
            <button onClick={() => handleNavClick('arcana')}>The Arcana</button>
            <button onClick={() => handleNavClick('beta')}>Join Beta</button>
          </motion.div>
        )}
      </AnimatePresence>
    </motion.nav>
  )
}
