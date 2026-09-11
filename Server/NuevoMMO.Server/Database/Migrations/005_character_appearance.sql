ALTER TABLE characters
    ADD COLUMN IF NOT EXISTS appearance_data TEXT NOT NULL DEFAULT 'v1|template.player||||||||';
