ALTER TABLE characters
    ADD COLUMN IF NOT EXISTS quest_data TEXT NOT NULL DEFAULT '{"quests":[]}';
