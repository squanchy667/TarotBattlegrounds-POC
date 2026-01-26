import { useEffect, useState, useRef } from 'react'
import {
  ArcanaCircle,
  CardFrame,
  SectionDivider,
  ConstellationBg,
  TribeSymbol,
  GearDecoration
} from './Illustrations'

// Tribe data for the showcase
const TRIBES = [
  {
    name: 'Pentacles',
    symbol: '⬡',
    color: '#C0A060',
    theme: 'Wealth & Economy',
    desc: 'Generate gold, scale into the late game',
    bonus: '+1 Gold per round at 4+ Pentacles'
  },
  {
    name: 'Cups',
    symbol: '◈',
    color: '#4090FF',
    theme: 'Healing & Restoration',
    desc: 'Regenerate health, outlast your rivals',
    bonus: '+2 HP restored per combat at 4+ Cups'
  },
  {
    name: 'Swords',
    symbol: '⚔',
    color: '#A03030',
    theme: 'Aggression & Damage',
    desc: 'Strike fast, dominate early rounds',
    bonus: '+2 Attack to all at 4+ Swords'
  },
  {
    name: 'Wands',
    symbol: '✦',
    color: '#8B5CF6',
    theme: 'Magic & Buffs',
    desc: 'Amplify your board with stacking power',
    bonus: '+1/+1 to random ally per turn at 4+ Wands'
  }
]

const SAMPLE_CARDS = [
  { name: 'Astral Sentinel', tribe: 'Cups', tier: 3, atk: 3, hp: 6 },
  { name: 'Coin Artificer', tribe: 'Pentacles', tier: 2, atk: 2, hp: 3 },
  { name: 'Blade Dancer', tribe: 'Swords', tier: 2, atk: 4, hp: 2 },
  { name: 'Flame Weaver', tribe: 'Wands', tier: 3, atk: 3, hp: 4 },
]

function App() {
  const [email, setEmail] = useState('')
  const [platform, setPlatform] = useState('PC')
  const [submitted, setSubmitted] = useState(false)
  const [activeCard, setActiveCard] = useState(0)
  const [isVisible, setIsVisible] = useState({})
  const heroRef = useRef(null)

  useEffect(() => {
    const interval = setInterval(() => {
      setActiveCard((prev) => (prev + 1) % SAMPLE_CARDS.length)
    }, 2500)
    return () => clearInterval(interval)
  }, [])

  useEffect(() => {
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            setIsVisible((prev) => ({ ...prev, [entry.target.id]: true }))
          }
        })
      },
      { threshold: 0.1 }
    )

    document.querySelectorAll('section[id]').forEach((section) => {
      observer.observe(section)
    })

    return () => observer.disconnect()
  }, [])

  const handleCtaClick = (label) => {
    if (typeof window !== 'undefined' && window.gtag) {
      window.gtag('event', 'cta_click', { event_category: 'engagement', event_label: label })
    }
  }

  const scrollTo = (id) => {
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth' })
  }

  const handleSubmit = (e) => {
    e.preventDefault()
    handleCtaClick('form_submit')

    // Submit to beehiiv via hidden iframe
    const iframe = document.getElementById('beehiiv-iframe')
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

    setSubmitted(true)
  }

  const tribeColor = {
    Pentacles: '#C0A060',
    Cups: '#4090FF',
    Swords: '#A03030',
    Wands: '#8B5CF6'
  }

  const currentCard = SAMPLE_CARDS[activeCard]

  return (
    <div className="page">
      {/* Cosmic Background with Blueprint Image */}
      <div className="cosmos" aria-hidden="true">
        <div className="blueprint-bg" />
        <div className="stars" />
        <div className="nebula nebula-1" />
        <div className="nebula nebula-2" />
        <ConstellationBg />
      </div>

      {/* Navigation */}
      <nav className="nav">
        <div className="nav-brand">
          <img src="/images/logo.png" alt="Tarot: Arcana Circle" className="brand-logo" />
          <div className="brand-text">
            <span className="brand-name">Tarot: Arcana Circle</span>
            <span className="brand-tag">Beta</span>
          </div>
        </div>
        <div className="nav-links">
          <button className="nav-link" onClick={() => scrollTo('gameplay')}>Gameplay</button>
          <button className="nav-link" onClick={() => scrollTo('arcana')}>The Arcana</button>
          <button className="btn btn-nav" onClick={() => { handleCtaClick('nav_beta'); scrollTo('beta'); }}>
            Join Beta
          </button>
        </div>
      </nav>

      {/* Hero Section */}
      <header className="hero" ref={heroRef}>
        <div className="hero-content">
          <div className="hero-text">
            <p className="eyebrow">
              <span className="eyebrow-icon">◇</span>
              The Arcana Circle opens
            </p>
            <h1 className="hero-title">
              Read the cards.
              <span className="title-accent">Outsmart the table.</span>
            </h1>
            <p className="hero-sub">
              An epic multiplayer arena where insight and wit win the game.
              Draft from rotating spreads, forge synergies, and let fate collide.
            </p>
            <div className="hero-chips">
              <span className="chip"><span className="chip-icon">⟳</span> Auto-Combat</span>
              <span className="chip"><span className="chip-icon">◈</span> 4 Arcana</span>
              <span className="chip"><span className="chip-icon">⚡</span> 20-min matches</span>
            </div>
            <div className="hero-cta">
              <button className="btn btn-primary" onClick={() => { handleCtaClick('hero_beta'); scrollTo('beta'); }}>
                <span>Join the Beta</span>
                <span className="btn-shine" />
              </button>
              <button className="btn btn-ghost" onClick={() => scrollTo('gameplay')}>
                How It Works
              </button>
            </div>
            <div className="hero-features">
              <div className="feature-line"><span className="line-marker" />Arcanists duel in rotating card spreads</div>
              <div className="feature-line"><span className="line-marker" />Every round sharpens your instincts</div>
              <div className="feature-line"><span className="line-marker" />Arcane tactics, crystal-clear outcomes</div>
            </div>
          </div>

          <div className="hero-visual">
            <div className="hero-image-wrapper">
              <img
                src="/images/hero-battle.png"
                alt="Epic battle in the Arcana Circle"
                className="hero-image"
              />
              <div className="hero-image-glow" />
            </div>
          </div>
        </div>

        <div className="scroll-hint" onClick={() => scrollTo('gameplay')}>
          <span className="scroll-text">Scroll to explore</span>
          <span className="scroll-arrow">↓</span>
        </div>
      </header>

      <main>
        {/* Gameplay Section */}
        <section id="gameplay" className={`section ${isVisible.gameplay ? 'visible' : ''}`}>
          <div className="section-header">
            <span className="section-tag">How it works</span>
            <h2>Three moves. Endless possibilities.</h2>
            <p className="section-desc">Each round follows a ritual rhythm—unveil, bind, collide.</p>
          </div>

          <div className="steps-grid">
            <article className="step-card">
              <div className="step-num">01</div>
              <div className="step-icon"><span>◇</span></div>
              <h3>Unveil the Arcana Circle</h3>
              <p>Choose from a rotating spread of cards each round. Spend gold wisely—the right pick can turn the tide.</p>
              <div className="step-stat">
                <span className="stat-label">Phase Duration</span>
                <span className="stat-value">35 seconds</span>
              </div>
            </article>

            <article className="step-card">
              <div className="step-num">02</div>
              <div className="step-icon"><span>⬡</span></div>
              <h3>Bind the Board</h3>
              <p>Assemble a seven-card formation and forge synergies. Position matters—tribes amplify each other.</p>
              <div className="step-stat">
                <span className="stat-label">Board Size</span>
                <span className="stat-value">7 Cards Max</span>
              </div>
            </article>

            <article className="step-card featured">
              <div className="step-num">03</div>
              <div className="step-icon"><span>✦</span></div>
              <h3>Let Fate Collide</h3>
              <p>Auto-battles resolve in dramatic, decisive turns. Your strategy fights for you.</p>
              <div className="step-stat">
                <span className="stat-label">Combat</span>
                <span className="stat-value">Fully Automated</span>
              </div>
            </article>
          </div>

          <div className="loop-diagram">
            <div className="loop-ring" />
            <div className="loop-center">Until 1 remains</div>
            <div className="loop-nodes">
              <span className="loop-node" style={{ '--angle': '-30deg' }}>Unveil</span>
              <span className="loop-node" style={{ '--angle': '90deg' }}>Bind</span>
              <span className="loop-node" style={{ '--angle': '210deg' }}>Collide</span>
            </div>
          </div>
        </section>

        <SectionDivider />

        {/* Experience Section */}
        <section id="experience" className={`section section-dark ${isVisible.experience ? 'visible' : ''}`}>
          <div className="experience-visual">
            <img src="/images/crystal-gem.png" alt="" className="crystal-float" />
          </div>
          <div className="section-header">
            <span className="section-tag">The experience</span>
            <h2>Simple to start. Satisfying to master.</h2>
          </div>

          <div className="experience-grid">
            <article className="exp-card">
              <div className="exp-icon">⚙</div>
              <h3>Clarity in every round</h3>
              <p>Every round sharpens your instincts and rewards smart choices. No hidden dice—just transparent strategy.</p>
            </article>
            <article className="exp-card">
              <div className="exp-icon">☾</div>
              <h3>Mysticism with intention</h3>
              <p>Arcana archetypes guide your strategy with clear, dramatic outcomes. Magic you can understand.</p>
            </article>
            <article className="exp-card">
              <div className="exp-icon">◈</div>
              <h3>Tactical identity</h3>
              <p>Shape a playstyle that feels distinctly yours. Aggressive, defensive, or synergy-focused—you decide.</p>
            </article>
          </div>
        </section>

        {/* Arcana/Tribes Section */}
        <section id="arcana" className={`section ${isVisible.arcana ? 'visible' : ''}`}>
          <div className="section-header">
            <span className="section-tag">The Four Arcana</span>
            <h2>Master the cosmic forces.</h2>
            <p className="section-desc">Each arcana represents a fundamental force. Combine them for devastating synergies.</p>
          </div>

          <div className="tribes-grid">
            {TRIBES.map((tribe) => (
              <article key={tribe.name} className="tribe-card" style={{ '--tribe-color': tribe.color }}>
                <div className="tribe-symbol-wrapper">
                  <TribeSymbol tribe={tribe.name} size={70} />
                </div>
                <h3>{tribe.name}</h3>
                <span className="tribe-theme">{tribe.theme}</span>
                <p>{tribe.desc}</p>
                <div className="tribe-bonus">
                  <span className="bonus-label">Synergy</span>
                  <span className="bonus-text">{tribe.bonus}</span>
                </div>
              </article>
            ))}
          </div>

          <div className="tribes-tip">
            <span className="tip-icon">💡</span>
            <p><strong>Pro tip:</strong> Cards can belong to multiple arcana. A Pentacles-Wands hybrid triggers both synergy bonuses!</p>
          </div>
        </section>

        {/* Beta Signup Section */}
        <section id="beta" className={`section section-glow ${isVisible.beta ? 'visible' : ''}`}>
          <div className="beta-bg" aria-hidden="true" />
          <img src="/images/crystal-gem-2.png" alt="" className="beta-crystal beta-crystal-left" aria-hidden="true" />
          <img src="/images/crystal-gem-3.png" alt="" className="beta-crystal beta-crystal-right" aria-hidden="true" />
          <div className="section-header">
            <span className="section-tag">Beta Signup</span>
            <h2>Step into the first circle of Arcanists.</h2>
            <p className="section-desc">Shape the arena with your feedback. Early access awaits.</p>
          </div>

          <div className="beta-content">
            {!submitted ? (
              <form className="beta-form" onSubmit={handleSubmit}>
                <div className="form-fields">
                  <label className="form-field">
                    <span className="field-label">Email</span>
                    <input
                      type="email"
                      value={email}
                      onChange={(e) => setEmail(e.target.value)}
                      placeholder="arcanist@domain.com"
                      required
                    />
                  </label>
                  <label className="form-field">
                    <span className="field-label">Platform</span>
                    <select value={platform} onChange={(e) => setPlatform(e.target.value)}>
                      <option value="PC">PC / Mac</option>
                      <option value="iOS">iOS</option>
                      <option value="Android">Android</option>
                    </select>
                  </label>
                </div>
                <button className="btn btn-primary btn-lg" type="submit">
                  <span>Join the Beta</span>
                  <span className="btn-shine" />
                </button>
                <p className="form-note">We respect your data. Unsubscribe anytime.</p>
              </form>
            ) : (
              <div className="beta-success">
                <div className="success-icon">✓</div>
                <h3>Sequence Initialized</h3>
                <p>Welcome, Arcanist. You'll receive word when the Circle opens.</p>
              </div>
            )}

            <div className="beta-stats">
              <div className="beta-stat">
                <span className="stat-num">2,847</span>
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
            </div>
          </div>
        </section>

        {/* Founders Section */}
        <section id="founders" className="section">
          <div className="founders-card">
            <div className="founders-quote">
              <span className="quote-mark">"</span>
              <p>
                Tarot: Arcana Circle began as a love letter to strategy—fast, readable, and deeply rewarding.
                We believe a game can feel mystical and still be crystal-clear, competitive and still inviting.
                The beta will shape the arcana, the pacing, and the soul of the arena.
                We'd be honored to have you at the table.
              </p>
            </div>
            <div className="founders-sig">
              <span className="sig-line" />
              <span className="sig-name">— The Tarot: Arcana Circle Team</span>
            </div>
          </div>
        </section>
      </main>

      {/* Footer */}
      <footer className="footer">
        <div className="footer-cta">
          <h2>The rite begins soon.</h2>
          <p>Join the beta and help shape the arena.</p>
          <button className="btn btn-primary" onClick={() => { handleCtaClick('footer_beta'); scrollTo('beta'); }}>
            <span>Join the Beta</span>
            <span className="btn-shine" />
          </button>
        </div>
        <div className="footer-bottom">
          <div className="footer-brand">
            <img src="/images/logo.png" alt="" className="footer-logo" />
            <span>Tarot: Arcana Circle</span>
          </div>
          <div className="footer-links">
            <button onClick={() => scrollTo('gameplay')}>Gameplay</button>
            <button onClick={() => scrollTo('arcana')}>Arcana</button>
            <button onClick={() => scrollTo('beta')}>Beta</button>
          </div>
          <p className="footer-copy">© 2026 Tarot: Arcana Circle. Your fate awaits.</p>
        </div>
      </footer>

      {/* Hidden iframe for beehiiv form submission */}
      <iframe
        id="beehiiv-iframe"
        name="beehiiv-iframe"
        style={{ display: 'none' }}
        title="beehiiv subscription"
      />
    </div>
  )
}

export default App
