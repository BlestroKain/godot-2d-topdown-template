-- Auth / character checkpoint schema. Applied by a future PostgreSQL migrator.
CREATE TABLE IF NOT EXISTS accounts (
    id uuid PRIMARY KEY,
    username text NOT NULL UNIQUE,
    password_hash text NOT NULL,
    created_at timestamptz NOT NULL
);

CREATE TABLE IF NOT EXISTS characters (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL REFERENCES accounts(id),
    name text NOT NULL UNIQUE,
    map_definition uuid NOT NULL,
    position_x real NOT NULL,
    position_y real NOT NULL,
    level integer NOT NULL DEFAULT 1,
    experience bigint NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS sessions (
    id uuid PRIMARY KEY,
    account_id uuid NOT NULL REFERENCES accounts(id),
    token text NOT NULL,
    created_at timestamptz NOT NULL,
    revoked_at timestamptz
);
