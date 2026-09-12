ALTER TABLE characters
    ADD COLUMN IF NOT EXISTS inventory_data TEXT NOT NULL DEFAULT '{"items":[],"equipment":[]}';
