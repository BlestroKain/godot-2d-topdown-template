CREATE UNIQUE INDEX IF NOT EXISTS ux_accounts_username_ci
    ON accounts ((lower(username)));

CREATE INDEX IF NOT EXISTS ix_characters_account_id
    ON characters (account_id);

CREATE INDEX IF NOT EXISTS ix_sessions_account_id
    ON sessions (account_id);

CREATE UNIQUE INDEX IF NOT EXISTS ux_sessions_token
    ON sessions (token);
