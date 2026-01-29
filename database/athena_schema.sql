-- =====================================================
-- TAROT BATTLEGROUNDS - AMAZON ATHENA SCHEMA
-- =====================================================
-- Note: Athena typically uses external tables on S3
-- This schema is for Iceberg tables or standard Athena tables

-- 1. TRIBES/FACTIONS
CREATE TABLE IF NOT EXISTS tribes (
    id              INT,
    name            VARCHAR(50),
    element         VARCHAR(20),
    theme_color     VARCHAR(7),
    description     VARCHAR(500),
    created_at      TIMESTAMP
);

-- 2. ABILITY TRIGGERS
CREATE TABLE IF NOT EXISTS ability_triggers (
    id              INT,
    name            VARCHAR(50),
    description     VARCHAR(200)
);

-- 3. ABILITY EFFECTS
CREATE TABLE IF NOT EXISTS ability_effects (
    id              INT,
    name            VARCHAR(100),
    description     VARCHAR(300),
    category        VARCHAR(30)
);

-- 4. CARDS
CREATE TABLE IF NOT EXISTS cards (
    id                  INT,
    name                VARCHAR(100),
    tier                SMALLINT,
    attack              SMALLINT,
    health              SMALLINT,
    ability_trigger_id  INT,
    ability_effect_id   INT,
    ability_value       SMALLINT,
    ability_text        VARCHAR(500),
    image_prompt        VARCHAR(2000),
    has_aegis           BOOLEAN,
    has_taunt           BOOLEAN,
    buy_cost_modifier   SMALLINT,
    sell_value_modifier SMALLINT,
    is_active           BOOLEAN,
    notes               VARCHAR(500),
    created_at          TIMESTAMP,
    updated_at          TIMESTAMP
);

-- 5. CARD_TRIBES (many-to-many)
CREATE TABLE IF NOT EXISTS card_tribes (
    card_id         INT,
    tribe_id        INT,
    is_primary      BOOLEAN
);

-- 6. ASSET TYPES
CREATE TABLE IF NOT EXISTS asset_types (
    id              INT,
    name            VARCHAR(50),
    description     VARCHAR(200),
    file_format     VARCHAR(10)
);

-- 7. ASSET STATUSES
CREATE TABLE IF NOT EXISTS asset_statuses (
    id              INT,
    name            VARCHAR(30),
    sort_order      SMALLINT,
    color           VARCHAR(7)
);

-- 8. ASSETS
CREATE TABLE IF NOT EXISTS assets (
    id              INT,
    name            VARCHAR(200),
    asset_type_id   INT,
    file_path       VARCHAR(500),
    canva_design_id VARCHAR(100),
    canva_url       VARCHAR(500),
    width           INT,
    height          INT,
    file_size_kb    INT,
    status_id       INT,
    version         SMALLINT,
    created_by      VARCHAR(100),
    created_at      TIMESTAMP,
    updated_at      TIMESTAMP
);

-- 9. CARD_ASSETS
CREATE TABLE IF NOT EXISTS card_assets (
    card_id         INT,
    asset_id        INT,
    asset_role      VARCHAR(30)
);

-- 10. TRIBE_ASSETS
CREATE TABLE IF NOT EXISTS tribe_assets (
    tribe_id        INT,
    asset_id        INT,
    asset_role      VARCHAR(30)
);

-- 11. ASSET_REVIEWS
CREATE TABLE IF NOT EXISTS asset_reviews (
    id              INT,
    asset_id        INT,
    previous_status INT,
    new_status      INT,
    reviewer        VARCHAR(100),
    review_notes    VARCHAR(1000),
    reviewed_at     TIMESTAMP
);

-- 12. GLOBAL_ASSETS
CREATE TABLE IF NOT EXISTS global_assets (
    id              INT,
    asset_id        INT,
    category        VARCHAR(50),
    applies_to      VARCHAR(50),
    description     VARCHAR(300)
);
