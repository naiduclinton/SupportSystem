-- ============================================================
-- Support Ticketing System — PostgreSQL Schema
-- ============================================================
-- Follows SOLID principles in structure:
--   - Tables have single responsibilities
--   - FK references are explicit and enforced
--   - Audit logging is centralised via trigger
--   - ENUM types centralise valid state values
-- ============================================================

-- ─────────────────────────────────────────
-- Extensions
-- ─────────────────────────────────────────
CREATE EXTENSION IF NOT EXISTS "pgcrypto";   -- gen_random_uuid()
CREATE EXTENSION IF NOT EXISTS "citext";     -- case-insensitive email


-- ─────────────────────────────────────────
-- ENUM Types
-- ─────────────────────────────────────────

CREATE TYPE ticket_status AS ENUM (
    'open',
    'in_progress',
    'pending',
    'resolved',
    'closed'
);

CREATE TYPE ticket_priority AS ENUM (
    'low',
    'medium',
    'high',
    'critical'
);

CREATE TYPE ticket_channel AS ENUM (
    'email',
    'portal',
    'phone',
    'chat',
    'api'
);

CREATE TYPE comment_type AS ENUM (
    'reply',        -- visible to customer
    'internal_note' -- agents only
);

CREATE TYPE user_role AS ENUM (
    'admin',
    'agent',
    'viewer'
);

CREATE TYPE sla_metric AS ENUM (
    'first_response',
    'resolution'
);

CREATE TYPE notification_channel AS ENUM (
    'email',
    'in_app',
    'sms'
);

CREATE TYPE automation_trigger AS ENUM (
    'ticket_created',
    'ticket_updated',
    'sla_breached',
    'status_changed',
    'idle_timeout'
);

CREATE TYPE automation_action AS ENUM (
    'assign_agent',
    'assign_team',
    'set_priority',
    'set_status',
    'send_notification',
    'add_tag',
    'escalate'
);


-- ─────────────────────────────────────────
-- Teams
-- ─────────────────────────────────────────

CREATE TABLE teams (
    id          UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    name        VARCHAR(100)    NOT NULL,
    description TEXT,
    created_at  TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    deleted_at  TIMESTAMPTZ
);


-- ─────────────────────────────────────────
-- Users (agents, admins, viewers)
-- ─────────────────────────────────────────

CREATE TABLE users (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    email           CITEXT      NOT NULL UNIQUE,
    full_name       VARCHAR(150) NOT NULL,
    avatar_url      TEXT,
    role            user_role   NOT NULL DEFAULT 'agent',
    team_id         UUID        REFERENCES teams(id) ON DELETE SET NULL,
    is_active       BOOLEAN     NOT NULL DEFAULT TRUE,
    -- Hashed via application layer (bcrypt/Argon2) — never store plaintext
    password_hash   TEXT,
    last_login_at   TIMESTAMPTZ,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at      TIMESTAMPTZ
);

CREATE INDEX idx_users_email       ON users(email);
CREATE INDEX idx_users_team_id     ON users(team_id);
CREATE INDEX idx_users_role        ON users(role);


-- ─────────────────────────────────────────
-- Customers (end-users raising tickets)
-- ─────────────────────────────────────────

CREATE TABLE customers (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    email           CITEXT      NOT NULL UNIQUE,
    full_name       VARCHAR(150),
    phone           VARCHAR(30),
    company         VARCHAR(150),
    external_id     VARCHAR(100),           -- CRM / HubSpot ID
    metadata        JSONB       DEFAULT '{}',
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at      TIMESTAMPTZ
);

CREATE INDEX idx_customers_email       ON customers(email);
CREATE INDEX idx_customers_external_id ON customers(external_id);
CREATE INDEX idx_customers_company     ON customers(company);


-- ─────────────────────────────────────────
-- Categories
-- ─────────────────────────────────────────

CREATE TABLE categories (
    id          UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    name        VARCHAR(100)    NOT NULL UNIQUE,
    description TEXT,
    parent_id   UUID            REFERENCES categories(id) ON DELETE SET NULL,
    created_at  TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);


-- ─────────────────────────────────────────
-- SLA Policies
-- ─────────────────────────────────────────

CREATE TABLE sla_policies (
    id                      UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    name                    VARCHAR(150)    NOT NULL,
    description             TEXT,
    priority                ticket_priority NOT NULL,
    -- Response/resolution targets in minutes
    first_response_minutes  INT             NOT NULL,
    resolution_minutes      INT             NOT NULL,
    -- Business hours only flag
    business_hours_only     BOOLEAN         NOT NULL DEFAULT TRUE,
    is_default              BOOLEAN         NOT NULL DEFAULT FALSE,
    created_at              TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

-- Ensure only one default per priority
CREATE UNIQUE INDEX idx_sla_default_priority
    ON sla_policies(priority)
    WHERE is_default = TRUE;


-- ─────────────────────────────────────────
-- Business Hours (for SLA calculation)
-- ─────────────────────────────────────────

CREATE TABLE business_hours (
    id          UUID    PRIMARY KEY DEFAULT gen_random_uuid(),
    day_of_week SMALLINT NOT NULL CHECK (day_of_week BETWEEN 0 AND 6), -- 0=Sun
    start_time  TIME    NOT NULL,
    end_time    TIME    NOT NULL,
    timezone    VARCHAR(60) NOT NULL DEFAULT 'Africa/Johannesburg',
    CONSTRAINT chk_business_hours_range CHECK (start_time < end_time)
);

CREATE UNIQUE INDEX idx_business_hours_day ON business_hours(day_of_week);


-- ─────────────────────────────────────────
-- Tickets
-- ─────────────────────────────────────────

CREATE TABLE tickets (
    id              UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    ticket_number   BIGSERIAL       NOT NULL UNIQUE,    -- human-readable #1042
    subject         VARCHAR(500)    NOT NULL,
    description     TEXT,
    status          ticket_status   NOT NULL DEFAULT 'open',
    priority        ticket_priority NOT NULL DEFAULT 'medium',
    channel         ticket_channel  NOT NULL DEFAULT 'portal',

    -- Relationships
    customer_id     UUID            NOT NULL REFERENCES customers(id),
    assignee_id     UUID            REFERENCES users(id) ON DELETE SET NULL,
    team_id         UUID            REFERENCES teams(id) ON DELETE SET NULL,
    category_id     UUID            REFERENCES categories(id) ON DELETE SET NULL,
    sla_policy_id   UUID            REFERENCES sla_policies(id) ON DELETE SET NULL,

    -- SLA tracking
    first_response_due_at   TIMESTAMPTZ,
    resolution_due_at       TIMESTAMPTZ,
    first_responded_at      TIMESTAMPTZ,
    resolved_at             TIMESTAMPTZ,
    closed_at               TIMESTAMPTZ,
    sla_breached            BOOLEAN NOT NULL DEFAULT FALSE,

    -- External references
    external_ref    VARCHAR(100),   -- e.g. Jira issue key
    metadata        JSONB           DEFAULT '{}',

    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    deleted_at      TIMESTAMPTZ
);

CREATE INDEX idx_tickets_status        ON tickets(status);
CREATE INDEX idx_tickets_priority      ON tickets(priority);
CREATE INDEX idx_tickets_customer_id   ON tickets(customer_id);
CREATE INDEX idx_tickets_assignee_id   ON tickets(assignee_id);
CREATE INDEX idx_tickets_team_id       ON tickets(team_id);
CREATE INDEX idx_tickets_category_id   ON tickets(category_id);
CREATE INDEX idx_tickets_created_at    ON tickets(created_at DESC);
CREATE INDEX idx_tickets_sla_due       ON tickets(first_response_due_at, resolution_due_at)
    WHERE status NOT IN ('resolved', 'closed');


-- ─────────────────────────────────────────
-- Tags
-- ─────────────────────────────────────────

CREATE TABLE tags (
    id      UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    name    VARCHAR(80) NOT NULL UNIQUE
);

CREATE TABLE ticket_tags (
    ticket_id   UUID    NOT NULL REFERENCES tickets(id) ON DELETE CASCADE,
    tag_id      UUID    NOT NULL REFERENCES tags(id) ON DELETE CASCADE,
    PRIMARY KEY (ticket_id, tag_id)
);


-- ─────────────────────────────────────────
-- Ticket Links (related / duplicate / blocked-by)
-- ─────────────────────────────────────────

CREATE TYPE ticket_link_type AS ENUM (
    'related',
    'duplicate',
    'blocks',
    'blocked_by'
);

CREATE TABLE ticket_links (
    id              UUID                PRIMARY KEY DEFAULT gen_random_uuid(),
    source_id       UUID                NOT NULL REFERENCES tickets(id) ON DELETE CASCADE,
    target_id       UUID                NOT NULL REFERENCES tickets(id) ON DELETE CASCADE,
    link_type       ticket_link_type    NOT NULL,
    created_at      TIMESTAMPTZ         NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_no_self_link CHECK (source_id <> target_id)
);

CREATE UNIQUE INDEX idx_ticket_links_unique
    ON ticket_links(source_id, target_id, link_type);


-- ─────────────────────────────────────────
-- Comments / Replies
-- ─────────────────────────────────────────

CREATE TABLE comments (
    id              UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    ticket_id       UUID            NOT NULL REFERENCES tickets(id) ON DELETE CASCADE,
    author_user_id  UUID            REFERENCES users(id) ON DELETE SET NULL,
    -- NULL author = customer reply
    author_customer_id UUID         REFERENCES customers(id) ON DELETE SET NULL,
    comment_type    comment_type    NOT NULL DEFAULT 'reply',
    body            TEXT            NOT NULL,
    is_edited       BOOLEAN         NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    deleted_at      TIMESTAMPTZ,
    CONSTRAINT chk_comment_author CHECK (
        (author_user_id IS NOT NULL) OR (author_customer_id IS NOT NULL)
    )
);

CREATE INDEX idx_comments_ticket_id ON comments(ticket_id);
CREATE INDEX idx_comments_created_at ON comments(ticket_id, created_at);


-- ─────────────────────────────────────────
-- Attachments
-- ─────────────────────────────────────────

CREATE TABLE attachments (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    ticket_id       UUID        REFERENCES tickets(id) ON DELETE CASCADE,
    comment_id      UUID        REFERENCES comments(id) ON DELETE CASCADE,
    filename        VARCHAR(255) NOT NULL,
    mime_type       VARCHAR(100),
    file_size_bytes BIGINT,
    storage_path    TEXT        NOT NULL,   -- S3 key or blob path
    uploaded_by_user_id UUID    REFERENCES users(id) ON DELETE SET NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_attachment_parent CHECK (
        (ticket_id IS NOT NULL) OR (comment_id IS NOT NULL)
    )
);


-- ─────────────────────────────────────────
-- CSAT (Customer Satisfaction) Surveys
-- ─────────────────────────────────────────

CREATE TABLE csat_surveys (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    ticket_id       UUID        NOT NULL UNIQUE REFERENCES tickets(id) ON DELETE CASCADE,
    customer_id     UUID        NOT NULL REFERENCES customers(id),
    score           SMALLINT    CHECK (score BETWEEN 1 AND 5),
    comment         TEXT,
    sent_at         TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    responded_at    TIMESTAMPTZ
);

CREATE INDEX idx_csat_ticket_id   ON csat_surveys(ticket_id);
CREATE INDEX idx_csat_customer_id ON csat_surveys(customer_id);


-- ─────────────────────────────────────────
-- Canned Responses (templated replies)
-- ─────────────────────────────────────────

CREATE TABLE canned_responses (
    id          UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    title       VARCHAR(200) NOT NULL,
    body        TEXT        NOT NULL,
    category_id UUID        REFERENCES categories(id) ON DELETE SET NULL,
    created_by  UUID        REFERENCES users(id) ON DELETE SET NULL,
    is_shared   BOOLEAN     NOT NULL DEFAULT TRUE,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);


-- ─────────────────────────────────────────
-- Automation Rules
-- ─────────────────────────────────────────

CREATE TABLE automation_rules (
    id              UUID                PRIMARY KEY DEFAULT gen_random_uuid(),
    name            VARCHAR(200)        NOT NULL,
    description     TEXT,
    is_active       BOOLEAN             NOT NULL DEFAULT TRUE,
    trigger_event   automation_trigger  NOT NULL,
    -- JSONB conditions: [{ "field": "priority", "op": "eq", "value": "critical" }]
    conditions      JSONB               NOT NULL DEFAULT '[]',
    -- JSONB actions: [{ "action": "assign_agent", "value": "<user_id>" }]
    actions         JSONB               NOT NULL DEFAULT '[]',
    execution_order INT                 NOT NULL DEFAULT 0,
    created_at      TIMESTAMPTZ         NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ         NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_automation_trigger ON automation_rules(trigger_event) WHERE is_active = TRUE;


-- ─────────────────────────────────────────
-- Notifications
-- ─────────────────────────────────────────

CREATE TABLE notifications (
    id              UUID                    PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id         UUID                    REFERENCES users(id) ON DELETE CASCADE,
    ticket_id       UUID                    REFERENCES tickets(id) ON DELETE CASCADE,
    channel         notification_channel    NOT NULL DEFAULT 'in_app',
    subject         VARCHAR(300),
    body            TEXT                    NOT NULL,
    is_read         BOOLEAN                 NOT NULL DEFAULT FALSE,
    sent_at         TIMESTAMPTZ,
    read_at         TIMESTAMPTZ,
    created_at      TIMESTAMPTZ             NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_notifications_user_unread
    ON notifications(user_id, created_at DESC)
    WHERE is_read = FALSE;


-- ─────────────────────────────────────────
-- Audit Log (centralised, immutable)
-- ─────────────────────────────────────────

CREATE TABLE audit_log (
    id              BIGSERIAL       PRIMARY KEY,
    table_name      VARCHAR(100)    NOT NULL,
    record_id       UUID            NOT NULL,
    action          VARCHAR(10)     NOT NULL CHECK (action IN ('INSERT','UPDATE','DELETE')),
    changed_by      UUID            REFERENCES users(id) ON DELETE SET NULL,
    old_values      JSONB,
    new_values      JSONB,
    ip_address      INET,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_audit_record   ON audit_log(table_name, record_id);
CREATE INDEX idx_audit_user     ON audit_log(changed_by, created_at DESC);
CREATE INDEX idx_audit_created  ON audit_log(created_at DESC);


-- ─────────────────────────────────────────
-- Knowledge Base Articles
-- ─────────────────────────────────────────

CREATE TABLE kb_articles (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    title           VARCHAR(300) NOT NULL,
    slug            VARCHAR(350) NOT NULL UNIQUE,
    body            TEXT        NOT NULL,
    category_id     UUID        REFERENCES categories(id) ON DELETE SET NULL,
    author_id       UUID        REFERENCES users(id) ON DELETE SET NULL,
    is_published    BOOLEAN     NOT NULL DEFAULT FALSE,
    view_count      INT         NOT NULL DEFAULT 0,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_kb_category    ON kb_articles(category_id);
CREATE INDEX idx_kb_published   ON kb_articles(is_published, updated_at DESC);


-- ─────────────────────────────────────────
-- Triggers: updated_at auto-maintenance
-- ─────────────────────────────────────────

CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$;

DO $$
DECLARE
    t TEXT;
BEGIN
    FOREACH t IN ARRAY ARRAY[
        'teams','users','customers','tickets','comments',
        'sla_policies','automation_rules','canned_responses',
        'kb_articles'
    ]
    LOOP
        EXECUTE format(
            'CREATE TRIGGER trg_%s_updated_at
             BEFORE UPDATE ON %I
             FOR EACH ROW EXECUTE FUNCTION set_updated_at()',
            t, t
        );
    END LOOP;
END;
$$;


-- ─────────────────────────────────────────
-- Triggers: Audit log on tickets
-- ─────────────────────────────────────────

CREATE OR REPLACE FUNCTION audit_ticket_changes()
RETURNS TRIGGER LANGUAGE plpgsql SECURITY DEFINER AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        INSERT INTO audit_log(table_name, record_id, action, new_values)
        VALUES ('tickets', NEW.id, 'INSERT', to_jsonb(NEW));
    ELSIF TG_OP = 'UPDATE' THEN
        INSERT INTO audit_log(table_name, record_id, action, old_values, new_values)
        VALUES ('tickets', NEW.id, 'UPDATE', to_jsonb(OLD), to_jsonb(NEW));
    ELSIF TG_OP = 'DELETE' THEN
        INSERT INTO audit_log(table_name, record_id, action, old_values)
        VALUES ('tickets', OLD.id, 'DELETE', to_jsonb(OLD));
    END IF;
    RETURN NULL;
END;
$$;

CREATE TRIGGER trg_tickets_audit
AFTER INSERT OR UPDATE OR DELETE ON tickets
FOR EACH ROW EXECUTE FUNCTION audit_ticket_changes();


-- ─────────────────────────────────────────
-- Views: Active ticket summary
-- ─────────────────────────────────────────

CREATE OR REPLACE VIEW v_ticket_summary AS
SELECT
    t.id,
    t.ticket_number,
    t.subject,
    t.status,
    t.priority,
    t.channel,
    t.sla_breached,
    t.created_at,
    t.updated_at,
    c.email          AS customer_email,
    c.full_name      AS customer_name,
    c.company        AS customer_company,
    u.full_name      AS assignee_name,
    tm.name          AS team_name,
    cat.name         AS category_name,
    -- SLA countdown in minutes (NULL if resolved/closed)
    CASE
        WHEN t.status NOT IN ('resolved','closed') AND t.resolution_due_at IS NOT NULL
        THEN EXTRACT(EPOCH FROM (t.resolution_due_at - NOW())) / 60
    END              AS resolution_minutes_remaining,
    -- SLA compliance %
    CASE
        WHEN t.resolution_due_at IS NOT NULL AND t.resolved_at IS NOT NULL
        THEN ROUND(
            100.0 * EXTRACT(EPOCH FROM (t.resolution_due_at - t.resolved_at))
                  / EXTRACT(EPOCH FROM (t.resolution_due_at - t.created_at)),
            1
        )
    END              AS sla_compliance_pct
FROM tickets t
JOIN customers c        ON c.id = t.customer_id
LEFT JOIN users u       ON u.id = t.assignee_id
LEFT JOIN teams tm      ON tm.id = t.team_id
LEFT JOIN categories cat ON cat.id = t.category_id
WHERE t.deleted_at IS NULL;


-- ─────────────────────────────────────────
-- Views: Agent workload
-- ─────────────────────────────────────────

CREATE OR REPLACE VIEW v_agent_workload AS
SELECT
    u.id,
    u.full_name,
    u.email,
    tm.name                                 AS team_name,
    COUNT(t.id) FILTER (WHERE t.status = 'open')        AS open_count,
    COUNT(t.id) FILTER (WHERE t.status = 'in_progress') AS in_progress_count,
    COUNT(t.id) FILTER (WHERE t.status = 'pending')     AS pending_count,
    COUNT(t.id) FILTER (WHERE t.sla_breached = TRUE
                          AND t.status NOT IN ('resolved','closed')) AS sla_breach_count,
    ROUND(AVG(
        EXTRACT(EPOCH FROM (t.resolved_at - t.created_at)) / 3600
    ) FILTER (WHERE t.resolved_at IS NOT NULL), 2)      AS avg_resolution_hours
FROM users u
LEFT JOIN teams tm   ON tm.id = u.team_id
LEFT JOIN tickets t  ON t.assignee_id = u.id AND t.deleted_at IS NULL
WHERE u.deleted_at IS NULL
  AND u.is_active = TRUE
  AND u.role IN ('agent','admin')
GROUP BY u.id, u.full_name, u.email, tm.name;


-- ─────────────────────────────────────────
-- Seed: Default SLA policies
-- ─────────────────────────────────────────

INSERT INTO sla_policies (name, priority, first_response_minutes, resolution_minutes, is_default) VALUES
    ('Critical SLA',  'critical', 60,   240,  TRUE),
    ('High SLA',      'high',     240,  1440, TRUE),
    ('Medium SLA',    'medium',   480,  4320, TRUE),
    ('Low SLA',       'low',      1440, 10080,TRUE);

-- ─────────────────────────────────────────
-- Seed: Default categories
-- ─────────────────────────────────────────

INSERT INTO categories (name, description) VALUES
    ('Billing',    'Payment, invoices, and subscription queries'),
    ('Technical',  'Bugs, errors, and product functionality issues'),
    ('Account',    'Login, access, and account management'),
    ('General',    'General enquiries and feedback');

-- ─────────────────────────────────────────
-- Seed: Business hours (Mon–Fri 08:00–17:00 SAST)
-- ─────────────────────────────────────────

INSERT INTO business_hours (day_of_week, start_time, end_time, timezone) VALUES
    (1, '08:00', '17:00', 'Africa/Johannesburg'),
    (2, '08:00', '17:00', 'Africa/Johannesburg'),
    (3, '08:00', '17:00', 'Africa/Johannesburg'),
    (4, '08:00', '17:00', 'Africa/Johannesburg'),
    (5, '08:00', '17:00', 'Africa/Johannesburg');
