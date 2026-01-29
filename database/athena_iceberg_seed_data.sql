-- =====================================================
-- TAROT BATTLEGROUNDS - AMAZON ATHENA ICEBERG SEED DATA
-- =====================================================
-- Run this after creating the Iceberg tables
-- =====================================================

USE tarot_battlegrounds;

-- =====================================================
-- REFERENCE DATA
-- =====================================================

-- TRIBES
INSERT INTO tribes (id, name, element, theme_color, description, created_at) VALUES
(1, 'Wands', 'Fire', '#FF6B35', 'Symbolizing creativity and passion, featuring heroes like The Innovator or The Visionary, embodying artistic and entrepreneurial spirits.', TIMESTAMP '2025-01-29 00:00:00'),
(2, 'Cups', 'Water', '#4A90D9', 'Representing emotions and intuition, heroes such as The Empath or The Mystic lead this faction, focusing on healing and psychic abilities.', TIMESTAMP '2025-01-29 00:00:00'),
(3, 'Swords', 'Air', '#A8A8A8', 'Associated with intellect and conflict, heroes include The Strategist or The Duelist, emphasizing tactical prowess and decisiveness.', TIMESTAMP '2025-01-29 00:00:00'),
(4, 'Pentacles', 'Earth', '#2E8B57', 'Connected to material aspects and stability, heroes like The Merchant or The Protector lead, focusing on wealth accumulation and defense.', TIMESTAMP '2025-01-29 00:00:00'),
(5, 'Neutral', NULL, '#9B59B6', 'Major Arcana themed cards that do not belong to any specific suit, representing universal forces and archetypes.', TIMESTAMP '2025-01-29 00:00:00');

-- ABILITY TRIGGERS
INSERT INTO ability_triggers (id, name, description) VALUES
(1, 'None', 'No ability trigger'),
(2, 'Battlecry', 'Triggers when the card is played from hand to board'),
(3, 'Deathrattle', 'Triggers when this card dies'),
(4, 'OnAttack', 'Triggers when this card attacks'),
(5, 'OnDamaged', 'Triggers when this card takes damage'),
(6, 'StartOfCombat', 'Triggers at the start of combat phase'),
(7, 'EndOfTurn', 'Triggers at the end of recruit phase'),
(8, 'Aura', 'Passive effect that is always active while card is on board'),
(9, 'OnKill', 'Triggers when this card kills an enemy'),
(10, 'OnAllyDeath', 'Triggers when a friendly minion dies'),
(11, 'EachRound', 'Triggers at the start of each round');

-- ABILITY EFFECTS
INSERT INTO ability_effects (id, name, description, category) VALUES
(1, 'None', 'No effect', NULL),
(2, 'BuffAdjacentAttack', 'Increase attack of adjacent minions', 'Buff'),
(3, 'BuffAdjacentHealth', 'Increase health of adjacent minions', 'Buff'),
(4, 'BuffAdjacentStats', 'Increase attack and health of adjacent minions', 'Buff'),
(5, 'BuffAllFriendlyAttack', 'Increase attack of all friendly minions', 'Buff'),
(6, 'BuffAllFriendlyHealth', 'Increase health of all friendly minions', 'Buff'),
(7, 'BuffAllFriendlyStats', 'Increase attack and health of all friendly minions', 'Buff'),
(8, 'BuffSelfAttack', 'Increase own attack', 'Buff'),
(9, 'BuffSelfStats', 'Increase own attack and health', 'Buff'),
(10, 'BuffTribeAttack', 'Increase attack of minions of same tribe', 'Buff'),
(11, 'BuffTribeStats', 'Increase attack and health of minions of same tribe', 'Buff'),
(12, 'GainAegis', 'Gain Aegis (shield that blocks one damage)', 'Defense'),
(13, 'GainCoins', 'Gain coins', 'Economy'),
(14, 'ReduceTierCost', 'Reduce cost of tier upgrade', 'Economy'),
(15, 'DamageRandomEnemy', 'Deal damage to a random enemy', 'Damage'),
(16, 'DamageAllEnemies', 'Deal damage to all enemies', 'Damage'),
(17, 'DamageAdjacent', 'Deal damage to adjacent enemies (cleave)', 'Damage'),
(18, 'BonusDamage', 'Deal bonus damage on attack', 'Damage'),
(19, 'IgnoreDefense', 'Attack ignores enemy defense', 'Damage'),
(20, 'InstantKillWeak', 'Chance to instantly kill weakened enemies', 'Damage'),
(21, 'HealSelf', 'Restore own health', 'Heal'),
(22, 'HealAllFriendly', 'Restore health of all friendly minions', 'Heal'),
(23, 'HealAdjacent', 'Restore health of adjacent minions', 'Heal'),
(24, 'Regeneration', 'Restore health each round', 'Heal'),
(25, 'Lifesteal', 'Heal for damage dealt', 'Heal'),
(26, 'LifestealShared', 'Share lifesteal with allies', 'Heal'),
(27, 'Taunt', 'Must be attacked first', 'Defense'),
(28, 'AbsorbDamage', 'Absorb damage for adjacent allies', 'Defense'),
(29, 'ReduceIncomingDamage', 'Reduce incoming damage', 'Defense'),
(30, 'ImmuneToSmallDamage', 'Immune to damage below threshold', 'Defense'),
(31, 'ShieldFromAoE', 'Protect from area damage', 'Defense'),
(32, 'DivertAttack', 'Redirect attack to self', 'Defense'),
(33, 'PreventAttacks', 'Temporarily prevent enemy attacks', 'Control'),
(34, 'RemoveFromCombat', 'Temporarily remove enemy from combat', 'Control'),
(35, 'WeakenEnemy', 'Reduce stats of enemy minion', 'Debuff'),
(36, 'TransferHealing', 'Steal enemy healing effects', 'Debuff'),
(37, 'Revive', 'Revive a fallen minion', 'Summon'),
(38, 'ReviveReduced', 'Revive with reduced stats', 'Summon'),
(39, 'ReviveSelf', 'Revive self with reduced stats', 'Summon'),
(40, 'SplitOnDeath', 'Split into smaller minions when defeated', 'Summon'),
(41, 'CombineMinions', 'Combine two weaker minions into stronger one', 'Summon'),
(42, 'ConvertDamageToGold', 'Convert received damage into gold', 'Economy'),
(43, 'GenerateResources', 'Generate additional resources', 'Economy'),
(44, 'RandomBuff', 'Apply random buffs to nearby minions', 'Random'),
(45, 'RandomEffect', 'Apply random buff or debuff each round', 'Random'),
(46, 'GainPowerFromSuits', 'Gain power based on number of different suits', 'Synergy'),
(47, 'QuickAttack', 'Attack first due to high speed', 'Combat'),
(48, 'MultiHit', 'Hit multiple random enemies', 'Combat');

-- ASSET TYPES
INSERT INTO asset_types (id, name, description, file_format) VALUES
(1, 'card_artwork', 'Main illustration for a card', 'png'),
(2, 'card_frame', 'Border/frame template for cards', 'png'),
(3, 'tribe_icon', 'Icon representing a tribe/faction', 'png'),
(4, 'tribe_logo', 'Full logo for a tribe/faction', 'png'),
(5, 'ui_element', 'User interface component', 'png'),
(6, 'background', 'Background image', 'png'),
(7, 'thumbnail', 'Small preview image', 'png'),
(8, 'alternate_art', 'Alternative artwork for a card', 'png'),
(9, 'card_back', 'Back side of card design', 'png'),
(10, 'ability_icon', 'Icon for abilities', 'png'),
(11, 'stat_icon', 'Icon for stats (attack/health)', 'png'),
(12, 'tier_badge', 'Badge indicating card tier', 'png');

-- ASSET STATUSES
INSERT INTO asset_statuses (id, name, sort_order, color) VALUES
(1, 'draft', 1, '#FFA500'),
(2, 'review', 2, '#3498DB'),
(3, 'revision_needed', 3, '#E74C3C'),
(4, 'approved', 4, '#2ECC71'),
(5, 'final', 5, '#27AE60');

-- =====================================================
-- CARDS - ALL 59 MINIONS
-- =====================================================

-- WANDS (Fire) - 12 cards (IDs 1-12)
INSERT INTO cards (id, name, tier, attack, health, ability_trigger_id, ability_effect_id, ability_value, ability_text, image_prompt, has_aegis, has_taunt, buy_cost_modifier, sell_value_modifier, is_active, notes, created_at, updated_at) VALUES
(1, 'Spark of Inspiration', 1, 1, 2, 8, 2, 1, 'Buffs adjacent minions'' attack', 'Spark of Inspiration: A ethereal spark of light manifesting as a small, glowing elemental figure with fiery tendrils, surrounded by swirling creative energy and abstract symbols of ideas like lightbulbs and flames. Tarot card style, vibrant reds and oranges, mystical fantasy art.', false, false, 0, 0, true, 'Tier 1 Wands - Aura buffer', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(2, 'Impulsive Apprentice', 1, 2, 1, 1, 47, NULL, 'Quick attacker, low HP', 'Impulsive Apprentice: A young wizard apprentice with wild red hair, wearing a simple robe singed at the edges, holding a flaming wand impulsively pointed forward, expression of eager excitement. Tarot-inspired illustration, dynamic pose, warm fire tones.', false, false, 0, 0, true, 'Tier 1 Wands - Glass cannon', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(3, 'Flame Dancer', 2, 3, 3, 8, 11, 1, 'Grants +1/+1 to other Wands minions', 'Flame Dancer: A graceful dancer enveloped in flowing flames that form elegant patterns, with lithe body adorned in fiery silks, performing a ritual dance. Mystical tarot aesthetic, orange and yellow hues, ethereal and passionate.', false, false, 0, 0, true, 'Tier 2 Wands - Tribe synergy', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(4, 'Torch Bearer', 2, 2, 4, 11, 8, 1, 'Gains power each round survived', 'Torch Bearer: A sturdy figure carrying a large torch that grows brighter over time, armored in leather with flame motifs, determined expression as if enduring battles. Fantasy tarot card, evolving light effects, earthy reds.', false, false, 0, 0, true, 'Tier 2 Wands - Scaling threat', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(5, 'Blazing Knight', 3, 4, 3, 9, 8, 1, 'Gains attack after killing a minion', 'Blazing Knight: A knight in blazing armor wielding a sword of fire, charging forward with increased ferocity after victories, helmet visor glowing with inner fire. Epic tarot style, intense red and gold palette.', false, false, 0, 0, true, 'Tier 3 Wands - Snowball attacker', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(6, 'Fiery Alchemist', 3, 3, 5, 2, 41, NULL, 'Combines two weaker minions into a stronger one', 'Fiery Alchemist: An alchemist at a cauldron merging essences, surrounded by potions and flames, transforming weak forms into strong ones, wise yet intense gaze. Alchemical symbols, tarot fantasy art, warm alchemical glow.', false, false, 0, 0, true, 'Tier 3 Wands - Utility combine', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(7, 'Phoenix Hatchling', 4, 5, 5, 3, 39, NULL, 'Revives once with reduced stats', 'Phoenix Hatchling: A baby phoenix emerging from ashes, feathers in shades of red and orange, with a rebirth aura, small but resilient. Mythical tarot illustration, symbolic revival theme.', false, false, 0, 0, true, 'Tier 4 Wands - Self revive', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(8, 'The Magician''s Disciple', 4, 4, 6, 8, 7, 1, 'Aura: +1/+1 to all allies', 'The Magician''s Disciple: A disciple mimicking a magician''s pose, channeling temporary magic buffs to allies, holding cards and wands, youthful and focused. Classic tarot Magician influence, magical energy bursts.', false, false, 0, 0, true, 'Tier 4 Wands - Team buffer', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(9, 'Volcanic Sage', 5, 7, 5, 6, 16, 3, 'AoE damage at combat start', 'Volcanic Sage: A wise sage standing atop a volcano, unleashing area fire blasts, robes flowing like lava, ancient staff in hand. Dramatic tarot art, volcanic reds and blacks.', false, false, 0, 0, true, 'Tier 5 Wands - AoE opener', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(10, 'Solar Warrior', 5, 6, 7, 8, 9, 2, 'Buffs during daytime rounds', 'Solar Warrior: A warrior bathed in sunlight, armor gleaming with solar flares, powering up during "day" themes, heroic stance. Radiant tarot style, bright yellows and oranges.', false, false, 0, 0, true, 'Tier 5 Wands - Conditional power', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(11, 'Dragon of Creation', 6, 8, 8, 2, 11, 3, 'Entry: +3/+3 to all Wands', 'Dragon of Creation: A majestic dragon of fire and creation, wings spread wide, buffing allies with creative energy, scales in fiery patterns. Grand tarot fantasy, dominant red tones.', false, false, 0, 0, true, 'Tier 6 Wands - Tribe finisher', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(12, 'Ace of Wands', 6, 10, 7, 8, 2, 3, 'Multiplies damage of adjacent allies significantly', 'Ace of Wands: The iconic Ace of Wands as a powerful staff bursting with multiplicative flames, surrounded by amplified allies, symbolic of growth and power. Traditional tarot design with enhanced drama.', false, false, 0, 0, true, 'Tier 6 Wands - Adjacent multiplier', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00');

-- CUPS (Water) - 12 cards (IDs 13-24)
INSERT INTO cards (id, name, tier, attack, health, ability_trigger_id, ability_effect_id, ability_value, ability_text, image_prompt, has_aegis, has_taunt, buy_cost_modifier, sell_value_modifier, is_active, notes, created_at, updated_at) VALUES
(13, 'Minor Healer', 1, 1, 3, 11, 24, 1, 'Small HP regeneration each round', 'Minor Healer: A gentle healer with a small cup overflowing with restorative water, simple robes, aura of minor regeneration, serene expression. Tarot Cups suit style, soft blues and greens.', false, false, 0, 0, true, 'Tier 1 Cups - Sustain', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(14, 'Intuitive Novice', 1, 2, 2, 8, 29, 1, 'Predicts and reduces incoming damage', 'Intuitive Novice: A novice psychic with intuitive eyes, holding a water shield that protects against threats, subtle water waves reducing damage. Mystical tarot art, intuitive symbols like eyes and waves.', false, false, 0, 0, true, 'Tier 1 Cups - Damage reduction', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(15, 'Soothing Siren', 2, 2, 4, 2, 33, 1, 'Prevents enemy attacks briefly', 'Soothing Siren: A siren with flowing hair like water, singing a calming song to prevent attacks, surrounded by peaceful waves. Enchanting tarot illustration, tranquil blue tones.', false, false, 0, 0, true, 'Tier 2 Cups - Crowd control', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(16, 'Peaceful Poet', 2, 3, 3, 10, 6, 1, 'Buffs ally defense when another minion dies', 'Peaceful Poet: A poet with a quill and cup, channeling emotions into defensive buffs upon losses, melancholic yet hopeful pose. Poetic tarot fantasy, emotional depth in colors.', false, false, 0, 0, true, 'Tier 2 Cups - Death trigger buffer', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(17, 'Moonlit Priestess', 3, 3, 6, 4, 22, 2, 'Heals entire party when it attacks', 'Moonlit Priestess: A priestess under moonlight, cup raised to heal the party, ethereal gown with lunar motifs. Nocturnal tarot style, silvery blues.', false, false, 0, 0, true, 'Tier 3 Cups - Attack healer', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(18, 'Empathic Guardian', 3, 4, 5, 8, 28, NULL, 'Takes damage for adjacent minions', 'Empathic Guardian: A guardian figure absorbing pain, arms outstretched protectively, cup symbolizing shared empathy. Heroic tarot art, protective water aura.', false, true, 0, 0, true, 'Tier 3 Cups - Adjacent protector', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(19, 'Oracle of Cups', 4, 4, 7, 11, 32, 1, 'Divert first hit each round', 'Oracle of Cups: An oracle with a prophetic cup, negating hits with foresight, veiled face and mystical eyes. Divinatory tarot design, deep indigo hues.', false, false, 0, 0, true, 'Tier 4 Cups - First hit immunity', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(20, 'Mystical Swan', 4, 3, 8, 8, 22, 1, 'Provides healing aura', 'Mystical Swan: An elegant swan transformed into a mystical being, radiating healing aura from its wings, serene lake background. Symbolic tarot illustration, pure whites and blues.', false, false, 0, 0, true, 'Tier 4 Cups - Passive healer', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(21, 'Serpent of the Depths', 5, 6, 7, 8, 26, NULL, 'Lifesteal shared with allies', 'Serpent of the Depths: A deep-sea serpent coiling around a cup, sharing lifesteal with allies, scales glistening with water. Mythical tarot fantasy, dark oceanic tones.', false, false, 0, 0, true, 'Tier 5 Cups - Team lifesteal', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(22, 'Queen of Cups', 5, 5, 9, 2, 22, 5, 'Massive healing, temporary invulnerability', 'Queen of Cups: The Queen of Cups as a regal figure with massive healing cup, granting invulnerability, compassionate and powerful gaze. Traditional tarot royalty, enriched blues.', true, false, 0, 0, true, 'Tier 5 Cups - Burst heal + aegis', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(23, 'Ace of Cups', 6, 7, 9, 4, 37, NULL, 'Revives a random fallen minion upon attack', 'Ace of Cups: The Ace of Cups overflowing with revival waters, bringing back fallen allies, symbolic of emotional abundance. Iconic tarot style with life-giving energy.', false, false, 0, 0, true, 'Tier 6 Cups - Attack revive', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(24, 'Leviathan Spirit', 6, 8, 10, 8, 36, NULL, 'Transfers enemy healing effects', 'Leviathan Spirit: A massive leviathan spirit emerging from depths, transferring healing with watery tendrils, imposing and ethereal. Epic tarot art, deep sea blues.', false, false, 0, 0, true, 'Tier 6 Cups - Heal steal', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00');

-- SWORDS (Air) - 12 cards (IDs 25-36)
INSERT INTO cards (id, name, tier, attack, health, ability_trigger_id, ability_effect_id, ability_value, ability_text, image_prompt, has_aegis, has_taunt, buy_cost_modifier, sell_value_modifier, is_active, notes, created_at, updated_at) VALUES
(25, 'Quickblade Scout', 1, 3, 1, 1, 47, NULL, 'High speed, low stats', 'Quickblade Scout: A swift scout with a lightweight sword, high-speed pose, minimal armor for agility. Tarot Swords suit, windy grays and silvers.', false, false, 0, 0, true, 'Tier 1 Swords - Fast glass cannon', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(26, 'Strategist''s Acolyte', 1, 2, 2, 4, 18, 1, 'Bonus damage vs damaged enemies', 'Strategist''s Acolyte: An acolyte studying tactics, sword poised for bonus strikes on weakened foes, thoughtful expression. Intellectual tarot illustration, strategic symbols.', false, false, 0, 0, true, 'Tier 1 Swords - Execute damage', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(27, 'Cunning Duelist', 2, 4, 2, 5, 8, 2, 'Increased damage after taking a hit', 'Cunning Duelist: A duelist in a fencing stance, sword gleaming after hits, cunning smile. Dynamic tarot art, air currents swirling.', false, false, 0, 0, true, 'Tier 2 Swords - Revenge attacker', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(28, 'Blade Juggler', 2, 3, 3, 4, 48, 2, 'Hits multiple random enemies', 'Blade Juggler: A juggler tossing multiple blades, hitting random enemies, circus-like flair with deadly intent. Entertaining yet fierce tarot style.', false, false, 0, 0, true, 'Tier 2 Swords - Multi-target', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(29, 'Storm Rider', 3, 5, 3, 6, 19, NULL, 'Initial strike ignores defense', 'Storm Rider: A rider on storm winds, sword striking first ignoring defenses, cloak billowing. Tempestuous tarot fantasy, stormy grays.', false, false, 0, 0, true, 'Tier 3 Swords - Armor pierce opener', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(30, 'Ruthless Mercenary', 3, 4, 4, 10, 8, 2, 'Gains strength after ally falls', 'Ruthless Mercenary: A mercenary with scarred armor, sword empowered by fallen allies, grim determination. Battle-hardened tarot design.', false, false, 0, 0, true, 'Tier 3 Swords - Ally death scaling', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(31, 'Wind Assassin', 4, 5, 5, 4, 20, NULL, 'Chance to instantly remove weak enemies', 'Wind Assassin: An assassin blending with winds, sword ready for instant kills, shadowy and swift. Stealthy tarot illustration, ethereal airs.', false, false, 0, 0, true, 'Tier 4 Swords - Execute chance', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(32, 'Master of Blades', 4, 6, 4, 9, 8, 1, 'Gains damage per enemy killed', 'Master of Blades: A master surrounded by floating blades, gaining power per kill, commanding presence. Martial tarot art, metallic silvers.', false, false, 0, 0, true, 'Tier 4 Swords - Kill stacker', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(33, 'Sky Commander', 5, 7, 5, 8, 5, 2, 'Boosts allies'' Attack', 'Sky Commander: A commander in the skies, boosting initiatives with wind commands, aerial pose. Leadership tarot style, high-altitude blues.', false, false, 0, 0, true, 'Tier 5 Swords - Attack aura', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(34, 'Queen of Swords', 5, 6, 7, 2, 35, 3, 'Weakens enemy''s strongest minion', 'Queen of Swords: The Queen of Swords weakening foes, sharp gaze and blade, intellectual authority. Traditional tarot, piercing clarity.', false, false, 0, 0, true, 'Tier 5 Swords - Enemy debuff', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(35, 'Ace of Swords', 6, 9, 6, 4, 20, NULL, 'Instantly eliminates weakened minions', 'Ace of Swords: The Ace of Swords as a triumphant blade eliminating the weak, symbolic of truth and conquest. Iconic tarot with decisive energy.', false, false, 0, 0, true, 'Tier 6 Swords - Guaranteed execute', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(36, 'Storm Dragon', 6, 8, 8, 6, 16, 4, 'AoE ignores defense', 'Storm Dragon: A dragon riding storms, unleashing ignoring-defense blasts, wings like thunderclouds. Majestic tarot fantasy, electric grays.', false, false, 0, 0, true, 'Tier 6 Swords - Piercing AoE', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00');

-- PENTACLES (Earth) - 12 cards (IDs 37-48)
INSERT INTO cards (id, name, tier, attack, health, ability_trigger_id, ability_effect_id, ability_value, ability_text, image_prompt, has_aegis, has_taunt, buy_cost_modifier, sell_value_modifier, is_active, notes, created_at, updated_at) VALUES
(37, 'Steadfast Farmer', 1, 1, 4, 1, 1, NULL, 'High HP, minimal attack', 'Steadfast Farmer: A farmer with earthy tools, high resilience, grounded stance amid fields. Tarot Pentacles suit, natural greens and browns.', false, false, 0, 0, true, 'Tier 1 Pentacles - Tank', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(38, 'Novice Herbalist', 1, 1, 3, 8, 3, 1, 'Buffs ally defense slightly', 'Novice Herbalist: A herbalist with plants and pentacle, buffing defenses, gentle nurturing pose. Botanical tarot art, healing herbs.', false, false, 0, 0, true, 'Tier 1 Pentacles - Defense buffer', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(39, 'Armored Guard', 2, 2, 5, 8, 28, NULL, 'Absorbs damage for allies', 'Armored Guard: A guard in heavy armor, absorbing hits, shield with pentacle emblem. Protective tarot illustration, solid earth tones.', false, true, 0, 0, true, 'Tier 2 Pentacles - Taunt tank', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(40, 'Young Druid', 2, 2, 4, 8, 24, 1, 'Healing aura and defense boost', 'Young Druid: A druid youth with nature aura, boosting health and defense, surrounded by vines. Mystical tarot style, forest greens.', false, false, 0, 0, true, 'Tier 2 Pentacles - Regen aura', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(41, 'Stone Golem', 3, 4, 6, 8, 30, 2, 'Immune to small damage', 'Stone Golem: A golem of rock, immune to minor harms, massive and unyielding form. Elemental tarot fantasy, rocky textures.', false, false, 0, 0, true, 'Tier 3 Pentacles - Damage threshold', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(42, 'Wealthy Trader', 3, 3, 5, 7, 43, 1, 'Generates additional resources', 'Wealthy Trader: A trader with coins and pentacles, generating resources, opulent attire. Prosperous tarot design, golden earth hues.', false, false, 0, 0, true, 'Tier 3 Pentacles - Economy engine', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(43, 'Forest Guardian', 4, 4, 8, 8, 31, NULL, 'Shields from AoE attacks', 'Forest Guardian: A guardian of woods, shielding from blasts, tree-like armor. Nature-protecting tarot art, deep greens.', false, false, 0, 0, true, 'Tier 4 Pentacles - AoE protection', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(44, 'King''s Treasurer', 4, 3, 9, 8, 14, 1, '-1 cost to tier upgrade', 'King''s Treasurer: A treasurer with vaults, enhancing upgrades, regal yet practical. Wealthy tarot illustration, coin motifs.', false, false, 0, 0, true, 'Tier 4 Pentacles - Upgrade discount', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(45, 'Colossus', 5, 5, 10, 3, 40, NULL, 'Splits into smaller minions when defeated', 'Colossus: A giant colossus splitting upon defeat, towering stone body. Imposing tarot style, earthen cracks.', false, false, 0, 0, true, 'Tier 5 Pentacles - Deathrattle split', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(46, 'Queen of Pentacles', 5, 4, 11, 2, 6, 4, 'Massively boosts allies'' health', 'Queen of Pentacles: The Queen of Pentacles boosting health massively, nurturing and abundant. Traditional tarot, fertile earth symbols.', false, false, 0, 0, true, 'Tier 5 Pentacles - Health buffer', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(47, 'Ace of Pentacles', 6, 7, 11, 11, 32, 1, 'Diverted attack for one attack', 'Ace of Pentacles: The Ace of Pentacles granting immunity, blooming with stability. Iconic tarot with protective glow.', true, false, 0, 0, true, 'Tier 6 Pentacles - Attack redirect', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(48, 'Ancient World Tree', 6, 6, 12, 11, 37, NULL, 'Revives fallen allies periodically', 'Ancient World Tree: A world tree reviving allies, roots and branches alive with life. Eternal tarot fantasy, verdant and ancient.', false, false, 0, 0, true, 'Tier 6 Pentacles - Periodic revive', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00');

-- HYBRID (Multi-tribe) - 5 cards (IDs 49-53)
INSERT INTO cards (id, name, tier, attack, health, ability_trigger_id, ability_effect_id, ability_value, ability_text, image_prompt, has_aegis, has_taunt, buy_cost_modifier, sell_value_modifier, is_active, notes, created_at, updated_at) VALUES
(49, 'Alchemist', 2, 3, 4, 5, 42, NULL, 'Converts received damage into gold in the next round', 'Alchemist (Wands/Pentacles): An alchemist blending fire and earth, converting damage to gold, cauldron with mixed elements. Hybrid tarot art, alchemical fusion.', false, false, 0, 0, true, 'Tier 2 Hybrid - Wands/Pentacles', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(50, 'Battle Cleric', 3, 4, 4, 4, 22, 2, 'Deals damage and heals allies slightly', 'Battle Cleric (Cups/Swords): A cleric in battle garb, sword and cup for damage and healing, balanced warrior-priest pose. Dual-suit tarot style.', false, false, 0, 0, true, 'Tier 3 Hybrid - Cups/Swords', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(51, 'Spellsword', 4, 5, 5, 4, 8, 1, 'Inflicts damage, increases own power each hit', 'Spellsword (Swords/Wands): A spellsword with enchanted blade, fire-infused strikes, growing power. Magical combat tarot illustration.', false, false, 0, 0, true, 'Tier 4 Hybrid - Swords/Wands', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(52, 'Earth Shaman', 5, 5, 8, 8, 22, 2, 'Defense, regenerates party health', 'Earth Shaman (Pentacles/Cups): A shaman of earth and water, regenerating with natural elements, tribal attire. Harmonious tarot fantasy.', false, false, 0, 0, true, 'Tier 5 Hybrid - Pentacles/Cups', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(53, 'Avatar of Balance', 6, 9, 9, 8, 46, NULL, 'Gains power from presence of multiple suits', 'Avatar of Balance (All Suits): An avatar embodying all suits, symbols of wands, cups, swords, pentacles orbiting, balanced and powerful. Ultimate tarot art, multicolored equilibrium.', false, false, 0, 0, true, 'Tier 6 Hybrid - All tribes', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00');

-- NEUTRAL (Major Arcana) - 6 cards (IDs 54-59)
INSERT INTO cards (id, name, tier, attack, health, ability_trigger_id, ability_effect_id, ability_value, ability_text, image_prompt, has_aegis, has_taunt, buy_cost_modifier, sell_value_modifier, is_active, notes, created_at, updated_at) VALUES
(54, 'The Fool''s Pet', 1, 2, 2, 1, 1, NULL, 'Simple stats, quick reinforcement', 'The Fool''s Pet (Neutral): A whimsical pet companion to The Fool, simple and quick, playful animal form. Tarot Major Arcana influence, lighthearted.', false, false, 0, 0, true, 'Tier 1 Neutral - Vanilla', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(55, 'Traveler', 2, 3, 3, 11, 44, NULL, 'Random minor buffs to nearby minions', 'Traveler (Neutral): A wandering traveler with random buffs, cloak and staff, adventurous stance. Nomadic tarot design.', false, false, 0, 0, true, 'Tier 2 Neutral - Random buffer', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(56, 'Wheel of Fortune', 3, 4, 4, 11, 45, NULL, 'Random effects (buff/debuff) each round', 'Wheel of Fortune (Neutral): The Wheel of Fortune as a spinning wheel with random effects, symbolic creatures around it. Classic tarot wheel, fateful colors.', false, false, 0, 0, true, 'Tier 3 Neutral - High variance', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(57, 'The Hermit', 4, 4, 6, 2, 34, NULL, 'Temporarily removes enemy minions from combat', 'The Hermit (Neutral): The Hermit removing foes temporarily, lantern in hand, solitary wisdom. Traditional tarot, introspective glow.', false, false, 0, 0, true, 'Tier 4 Neutral - Enemy banish', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(58, 'Strength', 5, 6, 6, 8, 4, 2, 'Buffs all adjacent minions significantly', 'Strength (Neutral): Strength buffing allies, figure taming a lion, courageous pose. Iconic tarot Major Arcana.', false, false, 0, 0, true, 'Tier 5 Neutral - Strong adjacent buff', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(59, 'Judgment', 6, 8, 8, 3, 38, NULL, 'Revives fallen minions with reduced stats', 'Judgment (Neutral): Judgment reviving with angelic trumpets, rising figures in reduced form. Apocalyptic tarot style, transformative energy.', false, false, 0, 0, true, 'Tier 6 Neutral - Mass revive reduced', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00');

-- =====================================================
-- CARD_TRIBES - Link cards to their tribes
-- =====================================================

-- WANDS (cards 1-12)
INSERT INTO card_tribes (card_id, tribe_id, is_primary) VALUES
(1, 1, true), (2, 1, true), (3, 1, true), (4, 1, true), (5, 1, true), (6, 1, true),
(7, 1, true), (8, 1, true), (9, 1, true), (10, 1, true), (11, 1, true), (12, 1, true);

-- CUPS (cards 13-24)
INSERT INTO card_tribes (card_id, tribe_id, is_primary) VALUES
(13, 2, true), (14, 2, true), (15, 2, true), (16, 2, true), (17, 2, true), (18, 2, true),
(19, 2, true), (20, 2, true), (21, 2, true), (22, 2, true), (23, 2, true), (24, 2, true);

-- SWORDS (cards 25-36)
INSERT INTO card_tribes (card_id, tribe_id, is_primary) VALUES
(25, 3, true), (26, 3, true), (27, 3, true), (28, 3, true), (29, 3, true), (30, 3, true),
(31, 3, true), (32, 3, true), (33, 3, true), (34, 3, true), (35, 3, true), (36, 3, true);

-- PENTACLES (cards 37-48)
INSERT INTO card_tribes (card_id, tribe_id, is_primary) VALUES
(37, 4, true), (38, 4, true), (39, 4, true), (40, 4, true), (41, 4, true), (42, 4, true),
(43, 4, true), (44, 4, true), (45, 4, true), (46, 4, true), (47, 4, true), (48, 4, true);

-- HYBRID CARDS (multiple tribes)
INSERT INTO card_tribes (card_id, tribe_id, is_primary) VALUES
(49, 1, true), (49, 4, false),   -- Alchemist: Wands + Pentacles
(50, 2, true), (50, 3, false),   -- Battle Cleric: Cups + Swords
(51, 3, true), (51, 1, false),   -- Spellsword: Swords + Wands
(52, 4, true), (52, 2, false),   -- Earth Shaman: Pentacles + Cups
(53, 1, true), (53, 2, false), (53, 3, false), (53, 4, false);  -- Avatar: All tribes

-- NEUTRAL (cards 54-59)
INSERT INTO card_tribes (card_id, tribe_id, is_primary) VALUES
(54, 5, true), (55, 5, true), (56, 5, true), (57, 5, true), (58, 5, true), (59, 5, true);

-- =====================================================
-- ASSETS - CANVA VISUAL ELEMENTS
-- =====================================================
-- Asset ID allocation:
--   1-59:    Card artwork (one per card)
--   60-69:   Tribe icons and logos
--   70-81:   Card frames (by tier and tribe)
--   82-89:   Card backs and misc card elements
--   90-99:   UI elements
--   100-109: Backgrounds
--   110-119: Ability icons
--   120-129: Stat icons and tier badges
--   200-209: RESERVED (10 empty slots for future use)
-- =====================================================

-- CARD ARTWORK (IDs 1-59) - One per card
INSERT INTO assets (id, name, asset_type_id, file_path, canva_design_id, canva_url, width, height, file_size_kb, status_id, version, created_by, created_at, updated_at) VALUES
-- Wands artwork (1-12)
(1, 'Spark of Inspiration - Artwork', 1, '/assets/cards/artwork/wands/spark_of_inspiration.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(2, 'Impulsive Apprentice - Artwork', 1, '/assets/cards/artwork/wands/impulsive_apprentice.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(3, 'Flame Dancer - Artwork', 1, '/assets/cards/artwork/wands/flame_dancer.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(4, 'Torch Bearer - Artwork', 1, '/assets/cards/artwork/wands/torch_bearer.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(5, 'Blazing Knight - Artwork', 1, '/assets/cards/artwork/wands/blazing_knight.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(6, 'Fiery Alchemist - Artwork', 1, '/assets/cards/artwork/wands/fiery_alchemist.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(7, 'Phoenix Hatchling - Artwork', 1, '/assets/cards/artwork/wands/phoenix_hatchling.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(8, 'The Magicians Disciple - Artwork', 1, '/assets/cards/artwork/wands/magicians_disciple.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(9, 'Volcanic Sage - Artwork', 1, '/assets/cards/artwork/wands/volcanic_sage.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(10, 'Solar Warrior - Artwork', 1, '/assets/cards/artwork/wands/solar_warrior.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(11, 'Dragon of Creation - Artwork', 1, '/assets/cards/artwork/wands/dragon_of_creation.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(12, 'Ace of Wands - Artwork', 1, '/assets/cards/artwork/wands/ace_of_wands.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),

-- Cups artwork (13-24)
(13, 'Minor Healer - Artwork', 1, '/assets/cards/artwork/cups/minor_healer.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(14, 'Intuitive Novice - Artwork', 1, '/assets/cards/artwork/cups/intuitive_novice.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(15, 'Soothing Siren - Artwork', 1, '/assets/cards/artwork/cups/soothing_siren.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(16, 'Peaceful Poet - Artwork', 1, '/assets/cards/artwork/cups/peaceful_poet.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(17, 'Moonlit Priestess - Artwork', 1, '/assets/cards/artwork/cups/moonlit_priestess.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(18, 'Empathic Guardian - Artwork', 1, '/assets/cards/artwork/cups/empathic_guardian.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(19, 'Oracle of Cups - Artwork', 1, '/assets/cards/artwork/cups/oracle_of_cups.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(20, 'Mystical Swan - Artwork', 1, '/assets/cards/artwork/cups/mystical_swan.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(21, 'Serpent of the Depths - Artwork', 1, '/assets/cards/artwork/cups/serpent_of_depths.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(22, 'Queen of Cups - Artwork', 1, '/assets/cards/artwork/cups/queen_of_cups.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(23, 'Ace of Cups - Artwork', 1, '/assets/cards/artwork/cups/ace_of_cups.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(24, 'Leviathan Spirit - Artwork', 1, '/assets/cards/artwork/cups/leviathan_spirit.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),

-- Swords artwork (25-36)
(25, 'Quickblade Scout - Artwork', 1, '/assets/cards/artwork/swords/quickblade_scout.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(26, 'Strategists Acolyte - Artwork', 1, '/assets/cards/artwork/swords/strategists_acolyte.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(27, 'Cunning Duelist - Artwork', 1, '/assets/cards/artwork/swords/cunning_duelist.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(28, 'Blade Juggler - Artwork', 1, '/assets/cards/artwork/swords/blade_juggler.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(29, 'Storm Rider - Artwork', 1, '/assets/cards/artwork/swords/storm_rider.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(30, 'Ruthless Mercenary - Artwork', 1, '/assets/cards/artwork/swords/ruthless_mercenary.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(31, 'Wind Assassin - Artwork', 1, '/assets/cards/artwork/swords/wind_assassin.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(32, 'Master of Blades - Artwork', 1, '/assets/cards/artwork/swords/master_of_blades.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(33, 'Sky Commander - Artwork', 1, '/assets/cards/artwork/swords/sky_commander.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(34, 'Queen of Swords - Artwork', 1, '/assets/cards/artwork/swords/queen_of_swords.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(35, 'Ace of Swords - Artwork', 1, '/assets/cards/artwork/swords/ace_of_swords.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(36, 'Storm Dragon - Artwork', 1, '/assets/cards/artwork/swords/storm_dragon.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),

-- Pentacles artwork (37-48)
(37, 'Steadfast Farmer - Artwork', 1, '/assets/cards/artwork/pentacles/steadfast_farmer.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(38, 'Novice Herbalist - Artwork', 1, '/assets/cards/artwork/pentacles/novice_herbalist.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(39, 'Armored Guard - Artwork', 1, '/assets/cards/artwork/pentacles/armored_guard.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(40, 'Young Druid - Artwork', 1, '/assets/cards/artwork/pentacles/young_druid.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(41, 'Stone Golem - Artwork', 1, '/assets/cards/artwork/pentacles/stone_golem.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(42, 'Wealthy Trader - Artwork', 1, '/assets/cards/artwork/pentacles/wealthy_trader.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(43, 'Forest Guardian - Artwork', 1, '/assets/cards/artwork/pentacles/forest_guardian.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(44, 'Kings Treasurer - Artwork', 1, '/assets/cards/artwork/pentacles/kings_treasurer.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(45, 'Colossus - Artwork', 1, '/assets/cards/artwork/pentacles/colossus.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(46, 'Queen of Pentacles - Artwork', 1, '/assets/cards/artwork/pentacles/queen_of_pentacles.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(47, 'Ace of Pentacles - Artwork', 1, '/assets/cards/artwork/pentacles/ace_of_pentacles.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(48, 'Ancient World Tree - Artwork', 1, '/assets/cards/artwork/pentacles/ancient_world_tree.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),

-- Hybrid artwork (49-53)
(49, 'Alchemist - Artwork', 1, '/assets/cards/artwork/hybrid/alchemist.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(50, 'Battle Cleric - Artwork', 1, '/assets/cards/artwork/hybrid/battle_cleric.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(51, 'Spellsword - Artwork', 1, '/assets/cards/artwork/hybrid/spellsword.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(52, 'Earth Shaman - Artwork', 1, '/assets/cards/artwork/hybrid/earth_shaman.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(53, 'Avatar of Balance - Artwork', 1, '/assets/cards/artwork/hybrid/avatar_of_balance.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),

-- Neutral artwork (54-59)
(54, 'The Fools Pet - Artwork', 1, '/assets/cards/artwork/neutral/fools_pet.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(55, 'Traveler - Artwork', 1, '/assets/cards/artwork/neutral/traveler.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(56, 'Wheel of Fortune - Artwork', 1, '/assets/cards/artwork/neutral/wheel_of_fortune.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(57, 'The Hermit - Artwork', 1, '/assets/cards/artwork/neutral/the_hermit.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(58, 'Strength - Artwork', 1, '/assets/cards/artwork/neutral/strength.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(59, 'Judgment - Artwork', 1, '/assets/cards/artwork/neutral/judgment.png', NULL, NULL, 1024, 1024, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00');

-- TRIBE ICONS AND LOGOS (IDs 60-69)
INSERT INTO assets (id, name, asset_type_id, file_path, canva_design_id, canva_url, width, height, file_size_kb, status_id, version, created_by, created_at, updated_at) VALUES
(60, 'Wands Tribe Icon', 3, '/assets/tribes/icons/wands_icon.png', NULL, NULL, 256, 256, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(61, 'Cups Tribe Icon', 3, '/assets/tribes/icons/cups_icon.png', NULL, NULL, 256, 256, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(62, 'Swords Tribe Icon', 3, '/assets/tribes/icons/swords_icon.png', NULL, NULL, 256, 256, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(63, 'Pentacles Tribe Icon', 3, '/assets/tribes/icons/pentacles_icon.png', NULL, NULL, 256, 256, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(64, 'Neutral Tribe Icon', 3, '/assets/tribes/icons/neutral_icon.png', NULL, NULL, 256, 256, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(65, 'Wands Tribe Logo', 4, '/assets/tribes/logos/wands_logo.png', NULL, NULL, 512, 512, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(66, 'Cups Tribe Logo', 4, '/assets/tribes/logos/cups_logo.png', NULL, NULL, 512, 512, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(67, 'Swords Tribe Logo', 4, '/assets/tribes/logos/swords_logo.png', NULL, NULL, 512, 512, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(68, 'Pentacles Tribe Logo', 4, '/assets/tribes/logos/pentacles_logo.png', NULL, NULL, 512, 512, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(69, 'Neutral Tribe Logo', 4, '/assets/tribes/logos/neutral_logo.png', NULL, NULL, 512, 512, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00');

-- CARD FRAMES - By Tier (IDs 70-75)
INSERT INTO assets (id, name, asset_type_id, file_path, canva_design_id, canva_url, width, height, file_size_kb, status_id, version, created_by, created_at, updated_at) VALUES
(70, 'Card Frame - Tier 1 (Common)', 2, '/assets/cards/frames/frame_tier1_common.png', NULL, NULL, 750, 1050, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(71, 'Card Frame - Tier 2 (Uncommon)', 2, '/assets/cards/frames/frame_tier2_uncommon.png', NULL, NULL, 750, 1050, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(72, 'Card Frame - Tier 3 (Rare)', 2, '/assets/cards/frames/frame_tier3_rare.png', NULL, NULL, 750, 1050, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(73, 'Card Frame - Tier 4 (Epic)', 2, '/assets/cards/frames/frame_tier4_epic.png', NULL, NULL, 750, 1050, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(74, 'Card Frame - Tier 5 (Legendary)', 2, '/assets/cards/frames/frame_tier5_legendary.png', NULL, NULL, 750, 1050, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(75, 'Card Frame - Tier 6 (Mythic)', 2, '/assets/cards/frames/frame_tier6_mythic.png', NULL, NULL, 750, 1050, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00');

-- CARD FRAMES - By Tribe (IDs 76-80)
INSERT INTO assets (id, name, asset_type_id, file_path, canva_design_id, canva_url, width, height, file_size_kb, status_id, version, created_by, created_at, updated_at) VALUES
(76, 'Card Frame - Wands Theme', 2, '/assets/cards/frames/frame_wands.png', NULL, NULL, 750, 1050, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(77, 'Card Frame - Cups Theme', 2, '/assets/cards/frames/frame_cups.png', NULL, NULL, 750, 1050, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(78, 'Card Frame - Swords Theme', 2, '/assets/cards/frames/frame_swords.png', NULL, NULL, 750, 1050, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(79, 'Card Frame - Pentacles Theme', 2, '/assets/cards/frames/frame_pentacles.png', NULL, NULL, 750, 1050, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(80, 'Card Frame - Neutral Theme', 2, '/assets/cards/frames/frame_neutral.png', NULL, NULL, 750, 1050, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(81, 'Card Frame - Hybrid Theme', 2, '/assets/cards/frames/frame_hybrid.png', NULL, NULL, 750, 1050, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00');

-- CARD BACKS AND MISC (IDs 82-89)
INSERT INTO assets (id, name, asset_type_id, file_path, canva_design_id, canva_url, width, height, file_size_kb, status_id, version, created_by, created_at, updated_at) VALUES
(82, 'Card Back - Standard', 9, '/assets/cards/backs/card_back_standard.png', NULL, NULL, 750, 1050, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(83, 'Card Back - Premium', 9, '/assets/cards/backs/card_back_premium.png', NULL, NULL, 750, 1050, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(84, 'Card Back - Wands', 9, '/assets/cards/backs/card_back_wands.png', NULL, NULL, 750, 1050, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(85, 'Card Back - Cups', 9, '/assets/cards/backs/card_back_cups.png', NULL, NULL, 750, 1050, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(86, 'Card Back - Swords', 9, '/assets/cards/backs/card_back_swords.png', NULL, NULL, 750, 1050, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(87, 'Card Back - Pentacles', 9, '/assets/cards/backs/card_back_pentacles.png', NULL, NULL, 750, 1050, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(88, 'Card Glow Effect - Golden', 5, '/assets/cards/effects/glow_golden.png', NULL, NULL, 800, 1100, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(89, 'Card Glow Effect - Legendary', 5, '/assets/cards/effects/glow_legendary.png', NULL, NULL, 800, 1100, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00');

-- UI ELEMENTS (IDs 90-99)
INSERT INTO assets (id, name, asset_type_id, file_path, canva_design_id, canva_url, width, height, file_size_kb, status_id, version, created_by, created_at, updated_at) VALUES
(90, 'UI Button - Primary', 5, '/assets/ui/buttons/btn_primary.png', NULL, NULL, 300, 80, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(91, 'UI Button - Secondary', 5, '/assets/ui/buttons/btn_secondary.png', NULL, NULL, 300, 80, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(92, 'UI Button - Buy', 5, '/assets/ui/buttons/btn_buy.png', NULL, NULL, 200, 60, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(93, 'UI Button - Sell', 5, '/assets/ui/buttons/btn_sell.png', NULL, NULL, 200, 60, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(94, 'UI Panel - Shop', 5, '/assets/ui/panels/panel_shop.png', NULL, NULL, 1200, 400, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(95, 'UI Panel - Board', 5, '/assets/ui/panels/panel_board.png', NULL, NULL, 1400, 600, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(96, 'UI Panel - Player Info', 5, '/assets/ui/panels/panel_player_info.png', NULL, NULL, 400, 200, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(97, 'UI Icon - Coin', 5, '/assets/ui/icons/icon_coin.png', NULL, NULL, 64, 64, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(98, 'UI Icon - Health', 5, '/assets/ui/icons/icon_health.png', NULL, NULL, 64, 64, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(99, 'UI Icon - Tier Star', 5, '/assets/ui/icons/icon_tier_star.png', NULL, NULL, 64, 64, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00');

-- BACKGROUNDS (IDs 100-109)
INSERT INTO assets (id, name, asset_type_id, file_path, canva_design_id, canva_url, width, height, file_size_kb, status_id, version, created_by, created_at, updated_at) VALUES
(100, 'Background - Main Menu', 6, '/assets/backgrounds/bg_main_menu.png', NULL, NULL, 1920, 1080, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(101, 'Background - Game Board', 6, '/assets/backgrounds/bg_game_board.png', NULL, NULL, 1920, 1080, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(102, 'Background - Combat Arena', 6, '/assets/backgrounds/bg_combat_arena.png', NULL, NULL, 1920, 1080, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(103, 'Background - Shop/Tavern', 6, '/assets/backgrounds/bg_shop_tavern.png', NULL, NULL, 1920, 1080, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(104, 'Background - Victory Screen', 6, '/assets/backgrounds/bg_victory.png', NULL, NULL, 1920, 1080, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(105, 'Background - Defeat Screen', 6, '/assets/backgrounds/bg_defeat.png', NULL, NULL, 1920, 1080, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(106, 'Background - Loading', 6, '/assets/backgrounds/bg_loading.png', NULL, NULL, 1920, 1080, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(107, 'Background - Card Reveal', 6, '/assets/backgrounds/bg_card_reveal.png', NULL, NULL, 1920, 1080, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(108, 'Background - Synergy Display', 6, '/assets/backgrounds/bg_synergy_display.png', NULL, NULL, 1920, 1080, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(109, 'Background - Tutorial', 6, '/assets/backgrounds/bg_tutorial.png', NULL, NULL, 1920, 1080, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00');

-- ABILITY ICONS (IDs 110-119)
INSERT INTO assets (id, name, asset_type_id, file_path, canva_design_id, canva_url, width, height, file_size_kb, status_id, version, created_by, created_at, updated_at) VALUES
(110, 'Ability Icon - Battlecry', 10, '/assets/abilities/icon_battlecry.png', NULL, NULL, 128, 128, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(111, 'Ability Icon - Deathrattle', 10, '/assets/abilities/icon_deathrattle.png', NULL, NULL, 128, 128, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(112, 'Ability Icon - Taunt', 10, '/assets/abilities/icon_taunt.png', NULL, NULL, 128, 128, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(113, 'Ability Icon - Aegis', 10, '/assets/abilities/icon_aegis.png', NULL, NULL, 128, 128, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(114, 'Ability Icon - Aura', 10, '/assets/abilities/icon_aura.png', NULL, NULL, 128, 128, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(115, 'Ability Icon - OnAttack', 10, '/assets/abilities/icon_on_attack.png', NULL, NULL, 128, 128, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(116, 'Ability Icon - Lifesteal', 10, '/assets/abilities/icon_lifesteal.png', NULL, NULL, 128, 128, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(117, 'Ability Icon - Cleave', 10, '/assets/abilities/icon_cleave.png', NULL, NULL, 128, 128, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(118, 'Ability Icon - Revive', 10, '/assets/abilities/icon_revive.png', NULL, NULL, 128, 128, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(119, 'Ability Icon - Random', 10, '/assets/abilities/icon_random.png', NULL, NULL, 128, 128, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00');

-- STAT ICONS AND TIER BADGES (IDs 120-129)
INSERT INTO assets (id, name, asset_type_id, file_path, canva_design_id, canva_url, width, height, file_size_kb, status_id, version, created_by, created_at, updated_at) VALUES
(120, 'Stat Icon - Attack Sword', 11, '/assets/stats/icon_attack.png', NULL, NULL, 64, 64, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(121, 'Stat Icon - Health Heart', 11, '/assets/stats/icon_health_heart.png', NULL, NULL, 64, 64, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(122, 'Tier Badge - 1 Star', 12, '/assets/tiers/badge_tier1.png', NULL, NULL, 96, 96, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(123, 'Tier Badge - 2 Stars', 12, '/assets/tiers/badge_tier2.png', NULL, NULL, 96, 96, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(124, 'Tier Badge - 3 Stars', 12, '/assets/tiers/badge_tier3.png', NULL, NULL, 96, 96, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(125, 'Tier Badge - 4 Stars', 12, '/assets/tiers/badge_tier4.png', NULL, NULL, 96, 96, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(126, 'Tier Badge - 5 Stars', 12, '/assets/tiers/badge_tier5.png', NULL, NULL, 96, 96, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(127, 'Tier Badge - 6 Stars', 12, '/assets/tiers/badge_tier6.png', NULL, NULL, 96, 96, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(128, 'Stat Frame - Attack Background', 11, '/assets/stats/frame_attack_bg.png', NULL, NULL, 80, 80, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(129, 'Stat Frame - Health Background', 11, '/assets/stats/frame_health_bg.png', NULL, NULL, 80, 80, NULL, 1, 1, 'Canva', TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00');

-- =====================================================
-- RESERVED ASSET SLOTS (IDs 200-209) - 10 empty records
-- =====================================================
INSERT INTO assets (id, name, asset_type_id, file_path, canva_design_id, canva_url, width, height, file_size_kb, status_id, version, created_by, created_at, updated_at) VALUES
(200, '[RESERVED] Slot 1', NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1, 1, NULL, TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(201, '[RESERVED] Slot 2', NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1, 1, NULL, TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(202, '[RESERVED] Slot 3', NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1, 1, NULL, TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(203, '[RESERVED] Slot 4', NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1, 1, NULL, TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(204, '[RESERVED] Slot 5', NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1, 1, NULL, TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(205, '[RESERVED] Slot 6', NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1, 1, NULL, TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(206, '[RESERVED] Slot 7', NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1, 1, NULL, TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(207, '[RESERVED] Slot 8', NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1, 1, NULL, TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(208, '[RESERVED] Slot 9', NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1, 1, NULL, TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00'),
(209, '[RESERVED] Slot 10', NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1, 1, NULL, TIMESTAMP '2025-01-29 00:00:00', TIMESTAMP '2025-01-29 00:00:00');

-- =====================================================
-- CARD_ASSETS - Link cards to their artwork
-- =====================================================
INSERT INTO card_assets (card_id, asset_id, asset_role) VALUES
-- Wands
(1, 1, 'artwork'), (2, 2, 'artwork'), (3, 3, 'artwork'), (4, 4, 'artwork'),
(5, 5, 'artwork'), (6, 6, 'artwork'), (7, 7, 'artwork'), (8, 8, 'artwork'),
(9, 9, 'artwork'), (10, 10, 'artwork'), (11, 11, 'artwork'), (12, 12, 'artwork'),
-- Cups
(13, 13, 'artwork'), (14, 14, 'artwork'), (15, 15, 'artwork'), (16, 16, 'artwork'),
(17, 17, 'artwork'), (18, 18, 'artwork'), (19, 19, 'artwork'), (20, 20, 'artwork'),
(21, 21, 'artwork'), (22, 22, 'artwork'), (23, 23, 'artwork'), (24, 24, 'artwork'),
-- Swords
(25, 25, 'artwork'), (26, 26, 'artwork'), (27, 27, 'artwork'), (28, 28, 'artwork'),
(29, 29, 'artwork'), (30, 30, 'artwork'), (31, 31, 'artwork'), (32, 32, 'artwork'),
(33, 33, 'artwork'), (34, 34, 'artwork'), (35, 35, 'artwork'), (36, 36, 'artwork'),
-- Pentacles
(37, 37, 'artwork'), (38, 38, 'artwork'), (39, 39, 'artwork'), (40, 40, 'artwork'),
(41, 41, 'artwork'), (42, 42, 'artwork'), (43, 43, 'artwork'), (44, 44, 'artwork'),
(45, 45, 'artwork'), (46, 46, 'artwork'), (47, 47, 'artwork'), (48, 48, 'artwork'),
-- Hybrid
(49, 49, 'artwork'), (50, 50, 'artwork'), (51, 51, 'artwork'), (52, 52, 'artwork'), (53, 53, 'artwork'),
-- Neutral
(54, 54, 'artwork'), (55, 55, 'artwork'), (56, 56, 'artwork'), (57, 57, 'artwork'), (58, 58, 'artwork'), (59, 59, 'artwork');

-- =====================================================
-- TRIBE_ASSETS - Link tribes to their icons and logos
-- =====================================================
INSERT INTO tribe_assets (tribe_id, asset_id, asset_role) VALUES
-- Icons
(1, 60, 'icon'),   -- Wands icon
(2, 61, 'icon'),   -- Cups icon
(3, 62, 'icon'),   -- Swords icon
(4, 63, 'icon'),   -- Pentacles icon
(5, 64, 'icon'),   -- Neutral icon
-- Logos
(1, 65, 'logo'),   -- Wands logo
(2, 66, 'logo'),   -- Cups logo
(3, 67, 'logo'),   -- Swords logo
(4, 68, 'logo'),   -- Pentacles logo
(5, 69, 'logo');   -- Neutral logo

-- =====================================================
-- GLOBAL_ASSETS - Shared assets not tied to specific cards
-- =====================================================
INSERT INTO global_assets (id, asset_id, category, applies_to, description) VALUES
-- Card frames by tier
(1, 70, 'card_frame', 'tier_1', 'Frame for all Tier 1 cards'),
(2, 71, 'card_frame', 'tier_2', 'Frame for all Tier 2 cards'),
(3, 72, 'card_frame', 'tier_3', 'Frame for all Tier 3 cards'),
(4, 73, 'card_frame', 'tier_4', 'Frame for all Tier 4 cards'),
(5, 74, 'card_frame', 'tier_5', 'Frame for all Tier 5 cards'),
(6, 75, 'card_frame', 'tier_6', 'Frame for all Tier 6 cards'),
-- Card frames by tribe
(7, 76, 'card_frame', 'wands_tribe', 'Frame for Wands tribe cards'),
(8, 77, 'card_frame', 'cups_tribe', 'Frame for Cups tribe cards'),
(9, 78, 'card_frame', 'swords_tribe', 'Frame for Swords tribe cards'),
(10, 79, 'card_frame', 'pentacles_tribe', 'Frame for Pentacles tribe cards'),
(11, 80, 'card_frame', 'neutral_tribe', 'Frame for Neutral tribe cards'),
(12, 81, 'card_frame', 'hybrid_tribe', 'Frame for Hybrid/multi-tribe cards'),
-- Card backs
(13, 82, 'card_back', 'all_cards', 'Standard card back for all cards'),
(14, 83, 'card_back', 'premium', 'Premium card back (unlockable)'),
(15, 84, 'card_back', 'wands_tribe', 'Wands themed card back'),
(16, 85, 'card_back', 'cups_tribe', 'Cups themed card back'),
(17, 86, 'card_back', 'swords_tribe', 'Swords themed card back'),
(18, 87, 'card_back', 'pentacles_tribe', 'Pentacles themed card back'),
-- Effects
(19, 88, 'card_effect', 'golden_cards', 'Golden glow effect for golden cards'),
(20, 89, 'card_effect', 'legendary_cards', 'Legendary glow effect'),
-- Backgrounds
(21, 100, 'background', 'main_menu', 'Main menu background'),
(22, 101, 'background', 'game_board', 'Game board background'),
(23, 102, 'background', 'combat', 'Combat arena background'),
(24, 103, 'background', 'shop', 'Shop/tavern background'),
(25, 104, 'background', 'victory', 'Victory screen background'),
(26, 105, 'background', 'defeat', 'Defeat screen background'),
(27, 106, 'background', 'loading', 'Loading screen background'),
(28, 107, 'background', 'card_reveal', 'Card reveal background'),
(29, 108, 'background', 'synergy', 'Synergy display background'),
(30, 109, 'background', 'tutorial', 'Tutorial background');
