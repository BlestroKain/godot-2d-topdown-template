-- Characters created before Tradition-at-creation remain NULL and are presented as legacy.
ALTER TABLE characters
    ADD COLUMN IF NOT EXISTS tradition_id UUID NULL;
