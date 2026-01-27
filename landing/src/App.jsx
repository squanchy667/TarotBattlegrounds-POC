import { useEffect, useState } from 'react'
import { ConstellationBg } from './Illustrations'
import { SectionDivider } from './Illustrations'

// Components
import Navigation from './components/Navigation'
import HeroSection from './components/HeroSection'
import GameplaySection from './components/GameplaySection'
import ExperienceSection from './components/ExperienceSection'
import ArcanaSection from './components/ArcanaSection'
import BetaSection from './components/BetaSection'
import FoundersSection from './components/FoundersSection'
import Footer from './components/Footer'
import MechanicalGears from './components/MechanicalGears'

function App() {
  const [isVisible, setIsVisible] = useState({})

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
      window.gtag('event', 'cta_click', {
        event_category: 'engagement',
        event_label: label,
      })
    }
  }

  const scrollTo = (id) => {
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth' })
  }

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

      {/* Decorative Mechanical Gears */}
      <div className="page-gears" aria-hidden="true">
        <MechanicalGears
          size="lg"
          className="page-gear page-gear-top-right"
          style={{ position: 'fixed', top: '-80px', right: '-80px', opacity: 0.08 }}
        />
        <MechanicalGears
          size="md"
          className="page-gear page-gear-bottom-left"
          style={{ position: 'fixed', bottom: '10%', left: '-60px', opacity: 0.06 }}
        />
      </div>

      {/* Navigation */}
      <Navigation onScrollTo={scrollTo} onCtaClick={handleCtaClick} />

      {/* Hero Section */}
      <HeroSection onScrollTo={scrollTo} onCtaClick={handleCtaClick} />

      <main>
        {/* Gameplay Section */}
        <GameplaySection isVisible={isVisible.gameplay} />

        <SectionDivider />

        {/* Experience Section */}
        <ExperienceSection isVisible={isVisible.experience} />

        {/* Arcana/Tribes Section */}
        <ArcanaSection isVisible={isVisible.arcana} />

        {/* Beta Signup Section */}
        <BetaSection isVisible={isVisible.beta} onCtaClick={handleCtaClick} />

        {/* Founders Section */}
        <FoundersSection />
      </main>

      {/* Footer */}
      <Footer onScrollTo={scrollTo} onCtaClick={handleCtaClick} />

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
