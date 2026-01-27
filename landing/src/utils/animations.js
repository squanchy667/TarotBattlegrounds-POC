import { gsap } from 'gsap'

/**
 * Animation presets for consistent motion design
 */
export const easings = {
  mechanical: 'cubic-bezier(0.18, 0.89, 0.32, 1.28)',
  smooth: 'power3.out',
  bounce: 'back.out(1.7)',
  elastic: 'elastic.out(1, 0.5)',
  snap: 'power4.out',
}

/**
 * Staggered word reveal animation for headlines
 * @param {HTMLElement} element - The text element to animate
 * @param {Object} options - Animation options
 */
export function animateTextReveal(element, { delay = 0, stagger = 0.05 } = {}) {
  if (!element) return

  const text = element.textContent
  const words = text.split(' ')

  element.innerHTML = words
    .map(
      (word) =>
        `<span style="display: inline-block; overflow: hidden;"><span style="display: inline-block; transform: translateY(100%);">${word}</span></span>`
    )
    .join(' ')

  const innerSpans = element.querySelectorAll('span > span')

  return gsap.to(innerSpans, {
    y: '0%',
    duration: 0.6,
    delay,
    stagger,
    ease: 'power3.out',
  })
}

/**
 * Number counting animation for stats
 * @param {HTMLElement} element - The element containing the number
 * @param {number} target - Target number to count to
 * @param {Object} options - Animation options
 */
export function animateCounter(element, target, { duration = 2, prefix = '', suffix = '' } = {}) {
  if (!element) return

  const counter = { value: 0 }

  return gsap.to(counter, {
    value: target,
    duration,
    ease: 'power2.out',
    onUpdate: () => {
      element.textContent = `${prefix}${Math.round(counter.value).toLocaleString()}${suffix}`
    },
  })
}

/**
 * Button shine sweep animation
 * @param {HTMLElement} button - The button element
 */
export function animateButtonShine(button) {
  if (!button) return

  const shine = document.createElement('span')
  shine.className = 'btn-shine-effect'
  shine.style.cssText = `
    position: absolute;
    top: 0;
    left: -100%;
    width: 100%;
    height: 100%;
    background: linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.3), transparent);
    pointer-events: none;
  `
  button.style.position = 'relative'
  button.style.overflow = 'hidden'
  button.appendChild(shine)

  return gsap.to(shine, {
    left: '100%',
    duration: 0.6,
    ease: 'power2.inOut',
    onComplete: () => shine.remove(),
  })
}

/**
 * Card 3D tilt effect on hover
 * @param {HTMLElement} card - The card element
 * @param {Object} options - Tilt options
 */
export function initCardTilt(card, { maxTilt = 10, glare = false } = {}) {
  if (!card) return

  const handleMouseMove = (e) => {
    const rect = card.getBoundingClientRect()
    const x = e.clientX - rect.left
    const y = e.clientY - rect.top
    const centerX = rect.width / 2
    const centerY = rect.height / 2

    const rotateX = ((y - centerY) / centerY) * -maxTilt
    const rotateY = ((x - centerX) / centerX) * maxTilt

    gsap.to(card, {
      rotateX,
      rotateY,
      duration: 0.3,
      ease: 'power2.out',
      transformPerspective: 1000,
    })

    if (glare) {
      const glareX = (x / rect.width) * 100
      const glareY = (y / rect.height) * 100
      card.style.setProperty('--glare-x', `${glareX}%`)
      card.style.setProperty('--glare-y', `${glareY}%`)
    }
  }

  const handleMouseLeave = () => {
    gsap.to(card, {
      rotateX: 0,
      rotateY: 0,
      duration: 0.5,
      ease: 'power2.out',
    })
  }

  card.addEventListener('mousemove', handleMouseMove)
  card.addEventListener('mouseleave', handleMouseLeave)

  return () => {
    card.removeEventListener('mousemove', handleMouseMove)
    card.removeEventListener('mouseleave', handleMouseLeave)
  }
}

/**
 * Floating animation for decorative elements
 * @param {HTMLElement} element - The element to animate
 * @param {Object} options - Float options
 */
export function animateFloat(element, { y = 20, duration = 3, delay = 0 } = {}) {
  if (!element) return

  return gsap.to(element, {
    y: -y,
    duration,
    delay,
    ease: 'sine.inOut',
    repeat: -1,
    yoyo: true,
  })
}

/**
 * Glow pulse animation
 * @param {HTMLElement} element - The element to animate
 */
export function animateGlow(element, { color = 'rgba(64, 144, 255, 0.5)', duration = 2 } = {}) {
  if (!element) return

  return gsap.to(element, {
    boxShadow: `0 0 60px ${color}`,
    duration,
    ease: 'sine.inOut',
    repeat: -1,
    yoyo: true,
  })
}

/**
 * Initialize all scroll-triggered section animations
 * @param {string} selector - CSS selector for sections
 */
export function initSectionAnimations(selector = '.section') {
  const sections = document.querySelectorAll(selector)

  sections.forEach((section, index) => {
    gsap.set(section, { opacity: 0, y: 50 })

    gsap.to(section, {
      scrollTrigger: {
        trigger: section,
        start: 'top 80%',
        toggleActions: 'play none none none',
      },
      opacity: 1,
      y: 0,
      duration: 0.8,
      delay: index * 0.1,
      ease: 'power3.out',
    })
  })
}

/**
 * Magnetic button effect
 * @param {HTMLElement} button - The button element
 */
export function initMagneticButton(button, { strength = 0.3 } = {}) {
  if (!button) return

  const handleMouseMove = (e) => {
    const rect = button.getBoundingClientRect()
    const x = e.clientX - rect.left - rect.width / 2
    const y = e.clientY - rect.top - rect.height / 2

    gsap.to(button, {
      x: x * strength,
      y: y * strength,
      duration: 0.3,
      ease: 'power2.out',
    })
  }

  const handleMouseLeave = () => {
    gsap.to(button, {
      x: 0,
      y: 0,
      duration: 0.5,
      ease: 'elastic.out(1, 0.5)',
    })
  }

  button.addEventListener('mousemove', handleMouseMove)
  button.addEventListener('mouseleave', handleMouseLeave)

  return () => {
    button.removeEventListener('mousemove', handleMouseMove)
    button.removeEventListener('mouseleave', handleMouseLeave)
  }
}

/**
 * Stagger cards entrance animation
 * @param {NodeList|Array} cards - The card elements
 */
export function animateCardsEntrance(cards, { stagger = 0.1, from = 'start' } = {}) {
  return gsap.from(cards, {
    opacity: 0,
    y: 60,
    rotateX: -15,
    transformOrigin: 'center top',
    duration: 0.8,
    stagger: {
      amount: stagger * cards.length,
      from,
    },
    ease: 'power3.out',
  })
}

export default {
  easings,
  animateTextReveal,
  animateCounter,
  animateButtonShine,
  initCardTilt,
  animateFloat,
  animateGlow,
  initSectionAnimations,
  initMagneticButton,
  animateCardsEntrance,
}
