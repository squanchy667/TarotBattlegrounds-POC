-- =====================================================
-- TAROT BATTLEGROUNDS - AMAZON ATHENA ICEBERG SCHEMA
-- =====================================================
-- Prerequisites:
--   1. Create an S3 bucket for your data
--   2. Replace 's3://your-bucket/tarot-battlegrounds/' with your actual bucket path
--   3. Ensure IAM permissions for Athena to read/write to S3
-- =====================================================

-- Set your database
CREATE DATABASE IF NOT EXISTS tarot_battlegrounds;
USE tarot_battlegrounds;

-- =====================================================
-- 1. TRIBES/FACTIONS
-- =====================================================
CREATE TABLE IF NOT EXISTS tribes (
    id              INT,
    name            STRING,
    element         STRING,
    theme_color     STRING,
    description     STRING,
    created_at      TIMESTAMP
)
LOCATION 's3://your-bucket/tarot-battlegrounds/tribes/'
TBLPROPERTIES (
    'table_type' = 'ICEBERG',
    'format' = 'PARQUET',
    'write_compression' = 'SNAPPY'
);

-- =====================================================
-- 2. ABILITY TRIGGERS
-- =====================================================
CREATE TABLE IF NOT EXISTS ability_triggers (
    id              INT,
    name            STRING,
    description     STRING
)
LOCATION 's3://your-bucket/tarot-battlegrounds/ability_triggers/'
TBLPROPERTIES (
    'table_type' = 'ICEBERG',
    'format' = 'PARQUET',
    'write_compression' = 'SNAPPY'
);

-- =====================================================
-- 3. ABILITY EFFECTS
-- =====================================================
CREATE TABLE IF NOT EXISTS ability_effects (
    id              INT,
    name            STRING,
    description     STRING,
    category        STRING
)
LOCATION 's3://your-bucket/tarot-battlegrounds/ability_effects/'
TBLPROPERTIES (
    'table_type' = 'ICEBERG',
    'format' = 'PARQUET',
    'write_compression' = 'SNAPPY'
);

-- =====================================================
-- 4. CARDS
-- =====================================================
CREATE TABLE IF NOT EXISTS cards (
    id                  INT,
    name                STRING,
    tier                SMALLINT,
    attack              SMALLINT,
    health              SMALLINT,
    ability_trigger_id  INT,
    ability_effect_id   INT,
    ability_value       SMALLINT,
    ability_text        STRING,
    image_prompt        STRING,
    has_aegis           BOOLEAN,
    has_taunt           BOOLEAN,
    buy_cost_modifier   SMALLINT,
    sell_value_modifier SMALLINT,
    is_active           BOOLEAN,
    notes               STRING,
    created_at          TIMESTAMP,
    updated_at          TIMESTAMP
)
LOCATION 's3://your-bucket/tarot-battlegrounds/cards/'
TBLPROPERTIES (
    'table_type' = 'ICEBERG',
    'format' = 'PARQUET',
    'write_compression' = 'SNAPPY'
);

-- =====================================================
-- 5. CARD_TRIBES (many-to-many)
-- =====================================================
CREATE TABLE IF NOT EXISTS card_tribes (
    card_id         INT,
    tribe_id        INT,
    is_primary      BOOLEAN
)
LOCATION 's3://your-bucket/tarot-battlegrounds/card_tribes/'
TBLPROPERTIES (
    'table_type' = 'ICEBERG',
    'format' = 'PARQUET',
    'write_compression' = 'SNAPPY'
);

-- =====================================================
-- 6. ASSET TYPES
-- =====================================================
CREATE TABLE IF NOT EXISTS asset_types (
    id              INT,
    name            STRING,
    description     STRING,
    file_format     STRING
)
LOCATION 's3://your-bucket/tarot-battlegrounds/asset_types/'
TBLPROPERTIES (
    'table_type' = 'ICEBERG',
    'format' = 'PARQUET',
    'write_compression' = 'SNAPPY'
);

-- =====================================================
-- 7. ASSET STATUSES
-- =====================================================
CREATE TABLE IF NOT EXISTS asset_statuses (
    id              INT,
    name            STRING,
    sort_order      SMALLINT,
    color           STRING
)
LOCATION 's3://your-bucket/tarot-battlegrounds/asset_statuses/'
TBLPROPERTIES (
    'table_type' = 'ICEBERG',
    'format' = 'PARQUET',
    'write_compression' = 'SNAPPY'
);

-- =====================================================
-- 8. ASSETS
-- =====================================================
CREATE TABLE IF NOT EXISTS assets (
    id              INT,
    name            STRING,
    asset_type_id   INT,
    file_path       STRING,
    canva_design_id STRING,
    canva_url       STRING,
    width           INT,
    height          INT,
    file_size_kb    INT,
    status_id       INT,
    version         SMALLINT,
    created_by      STRING,
    created_at      TIMESTAMP,
    updated_at      TIMESTAMP
)
LOCATION 's3://your-bucket/tarot-battlegrounds/assets/'
TBLPROPERTIES (
    'table_type' = 'ICEBERG',
    'format' = 'PARQUET',
    'write_compression' = 'SNAPPY'
);

-- =====================================================
-- 9. CARD_ASSETS
-- =====================================================
CREATE TABLE IF NOT EXISTS card_assets (
    card_id         INT,
    asset_id        INT,
    asset_role      STRING
)
LOCATION 's3://your-bucket/tarot-battlegrounds/card_assets/'
TBLPROPERTIES (
    'table_type' = 'ICEBERG',
    'format' = 'PARQUET',
    'write_compression' = 'SNAPPY'
);

-- =====================================================
-- 10. TRIBE_ASSETS
-- =====================================================
CREATE TABLE IF NOT EXISTS tribe_assets (
    tribe_id        INT,
    asset_id        INT,
    asset_role      STRING
)
LOCATION 's3://your-bucket/tarot-battlegrounds/tribe_assets/'
TBLPROPERTIES (
    'table_type' = 'ICEBERG',
    'format' = 'PARQUET',
    'write_compression' = 'SNAPPY'
);

-- =====================================================
-- 11. ASSET_REVIEWS
-- =====================================================
CREATE TABLE IF NOT EXISTS asset_reviews (
    id              INT,
    asset_id        INT,
    previous_status INT,
    new_status      INT,
    reviewer        STRING,
    review_notes    STRING,
    reviewed_at     TIMESTAMP
)
LOCATION 's3://your-bucket/tarot-battlegrounds/asset_reviews/'
TBLPROPERTIES (
    'table_type' = 'ICEBERG',
    'format' = 'PARQUET',
    'write_compression' = 'SNAPPY'
);

-- =====================================================
-- 12. GLOBAL_ASSETS
-- =====================================================
CREATE TABLE IF NOT EXISTS global_assets (
    id              INT,
    asset_id        INT,
    category        STRING,
    applies_to      STRING,
    description     STRING
)
LOCATION 's3://your-bucket/tarot-battlegrounds/global_assets/'
TBLPROPERTIES (
    'table_type' = 'ICEBERG',
    'format' = 'PARQUET',
    'write_compression' = 'SNAPPY'
);

-- =====================================================
-- VIEWS (Athena supports views over Iceberg tables)
-- =====================================================

-- Full card view with tribe names
CREATE OR REPLACE VIEW v_cards_full AS
SELECT
    c.id,
    c.name,
    c.tier,
    c.attack,
    c.health,
    c.ability_text,
    c.image_prompt,
    c.has_aegis,
    c.has_taunt,
    at.name AS ability_trigger,
    ae.name AS ability_effect,
    c.ability_value,
    c.is_active
FROM cards c
LEFT JOIN ability_triggers at ON c.ability_trigger_id = at.id
LEFT JOIN ability_effects ae ON c.ability_effect_id = ae.id;

-- Asset workflow status view
CREATE OR REPLACE VIEW v_asset_status AS
SELECT
    a.id,
    a.name,
    at.name AS asset_type,
    s.name AS status,
    a.version,
    a.file_path,
    a.canva_design_id,
    a.canva_url,
    a.updated_at
FROM assets a
LEFT JOIN asset_types at ON a.asset_type_id = at.id
LEFT JOIN asset_statuses s ON a.status_id = s.id;

-- Cards with their artwork
CREATE OR REPLACE VIEW v_cards_with_artwork AS
SELECT
    c.id AS card_id,
    c.name AS card_name,
    c.tier,
    c.attack,
    c.health,
    a.id AS asset_id,
    a.file_path,
    a.canva_design_id,
    s.name AS asset_status
FROM cards c
LEFT JOIN card_assets ca ON c.id = ca.card_id AND ca.asset_role = 'artwork'
LEFT JOIN assets a ON ca.asset_id = a.id
LEFT JOIN asset_statuses s ON a.status_id = s.id;
