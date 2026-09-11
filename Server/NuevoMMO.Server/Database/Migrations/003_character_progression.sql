ALTER TABLE characters ADD COLUMN IF NOT EXISTS available_attribute_points integer NOT NULL DEFAULT 0;
ALTER TABLE characters ADD COLUMN IF NOT EXISTS strength integer NOT NULL DEFAULT 10;
ALTER TABLE characters ADD COLUMN IF NOT EXISTS intelligence integer NOT NULL DEFAULT 10;
ALTER TABLE characters ADD COLUMN IF NOT EXISTS agility integer NOT NULL DEFAULT 10;
ALTER TABLE characters ADD COLUMN IF NOT EXISTS spirit integer NOT NULL DEFAULT 10;
ALTER TABLE characters ADD COLUMN IF NOT EXISTS vitality integer NOT NULL DEFAULT 10;
ALTER TABLE characters ADD COLUMN IF NOT EXISTS current_health integer NULL;
ALTER TABLE characters ADD COLUMN IF NOT EXISTS current_mana integer NULL;

-- Personajes creados antes de la progresión distribuible conservan los puntos
-- que corresponden a su nivel en vez de perderlos durante la migración.
UPDATE characters
SET available_attribute_points = GREATEST((level - 1) * 3, 0)
WHERE available_attribute_points = 0
  AND strength = 10 AND intelligence = 10 AND agility = 10 AND spirit = 10 AND vitality = 10;
