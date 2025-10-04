-- Live Support Ticket Dashboard - Database Schema
-- PostgreSQL initialization script

-- Enable UUID extension for generating UUIDs
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Create agents table
CREATE TABLE IF NOT EXISTS agents (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(200) NOT NULL,
    email VARCHAR(320) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Create sessions table for chat sessions
CREATE TABLE IF NOT EXISTS sessions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL,
    assigned_agent_id UUID REFERENCES agents(id) ON DELETE SET NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'active' CHECK (status IN ('active', 'closed')),
    metadata JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_activity_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Create messages table for chat messages
CREATE TABLE IF NOT EXISTS messages (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    session_id UUID NOT NULL REFERENCES sessions(id) ON DELETE CASCADE,
    sender_id UUID NOT NULL,
    sender_type VARCHAR(10) NOT NULL CHECK (sender_type IN ('agent', 'user')),
    text TEXT NOT NULL,
    attachments JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Create tickets table
CREATE TABLE IF NOT EXISTS tickets (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    title VARCHAR(200) NOT NULL,
    description TEXT,
    priority VARCHAR(20) NOT NULL CHECK (priority IN ('Low', 'Medium', 'High', 'Critical')),
    status VARCHAR(20) NOT NULL DEFAULT 'Open' CHECK (status IN ('Open', 'InProgress', 'Resolved')),
    assigned_agent_id UUID REFERENCES agents(id) ON DELETE SET NULL,
    sla_due_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Create ticket history table for audit trail
CREATE TABLE IF NOT EXISTS ticket_history (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    ticket_id UUID NOT NULL REFERENCES tickets(id) ON DELETE CASCADE,
    action VARCHAR(100) NOT NULL,
    details TEXT,
    agent_name VARCHAR(200) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Create indexes for better query performance
CREATE INDEX IF NOT EXISTS idx_tickets_status ON tickets(status);
CREATE INDEX IF NOT EXISTS idx_tickets_priority ON tickets(priority);
CREATE INDEX IF NOT EXISTS idx_tickets_created_at ON tickets(created_at DESC);
CREATE INDEX IF NOT EXISTS idx_tickets_assigned_agent ON tickets(assigned_agent_id);
CREATE INDEX IF NOT EXISTS idx_tickets_title_search ON tickets USING gin(to_tsvector('english', title));
CREATE INDEX IF NOT EXISTS idx_tickets_sla_due ON tickets(sla_due_at);
CREATE INDEX IF NOT EXISTS idx_ticket_history_ticket_id ON ticket_history(ticket_id);
CREATE INDEX IF NOT EXISTS idx_ticket_history_created_at ON ticket_history(created_at DESC);
CREATE INDEX IF NOT EXISTS idx_sessions_user_id ON sessions(user_id);
CREATE INDEX IF NOT EXISTS idx_sessions_assigned_agent ON sessions(assigned_agent_id);
CREATE INDEX IF NOT EXISTS idx_sessions_status ON sessions(status);
CREATE INDEX IF NOT EXISTS idx_sessions_last_activity ON sessions(last_activity_at DESC);
CREATE INDEX IF NOT EXISTS idx_messages_session_id ON messages(session_id);
CREATE INDEX IF NOT EXISTS idx_messages_created_at ON messages(created_at DESC);
CREATE INDEX IF NOT EXISTS idx_messages_sender ON messages(sender_id, sender_type);

-- Create updated_at trigger function
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Function to automatically create history entry on ticket changes
CREATE OR REPLACE FUNCTION create_ticket_history()
RETURNS TRIGGER AS $$
BEGIN
    -- On INSERT (ticket creation)
    IF TG_OP = 'INSERT' THEN
        INSERT INTO ticket_history (ticket_id, action, details, agent_name)
        VALUES (NEW.id, 'created ticket', 'Ticket created from customer report', 'System');
        RETURN NEW;
    END IF;

    -- On UPDATE
    IF TG_OP = 'UPDATE' THEN
        -- Status change
        IF OLD.status != NEW.status THEN
            INSERT INTO ticket_history (ticket_id, action, details, agent_name)
            VALUES (NEW.id, 'updated status',
                   'Changed status from ' || OLD.status || ' to ' || NEW.status, 'System');
        END IF;

        -- Assignment change
        IF OLD.assigned_agent_id IS DISTINCT FROM NEW.assigned_agent_id THEN
            INSERT INTO ticket_history (ticket_id, action, details, agent_name)
            VALUES (NEW.id, 'updated assignment', 'Ticket assignment changed', 'System');
        END IF;

        RETURN NEW;
    END IF;

    RETURN NULL;
END;
$$ language 'plpgsql';

-- Create triggers to automatically update the updated_at column
CREATE TRIGGER update_agents_updated_at BEFORE UPDATE ON agents
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_tickets_updated_at BEFORE UPDATE ON tickets
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_sessions_updated_at BEFORE UPDATE ON sessions
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Create trigger for automatic ticket history
CREATE TRIGGER ticket_history_trigger
    AFTER INSERT OR UPDATE ON tickets
    FOR EACH ROW EXECUTE FUNCTION create_ticket_history();

-- Insert sample agents for testing
INSERT INTO agents (name, email) VALUES
    ('John Smith', 'john.smith@company.com'),
    ('Sarah Wilson', 'sarah.wilson@company.com'),
    ('Mike Johnson', 'mike.johnson@company.com'),
    ('Emily Davis', 'emily.davis@company.com'),
    ('Alex Brown', 'alex.brown@company.com')
ON CONFLICT (email) DO NOTHING;

-- Insert realistic sample tickets that resemble your mock data
-- Critical priority tickets (4 hour SLA)
INSERT INTO tickets (title, description, priority, status, assigned_agent_id, sla_due_at, created_at, updated_at)
SELECT
    'Login page not loading properly',
    'Users are reporting that the login page takes too long to load and sometimes shows a blank screen. This is affecting multiple users across different browsers.',
    'Critical',
    'Open',
    a.id,
    NOW() + INTERVAL '4 hours',
    NOW() - INTERVAL '45 minutes',
    NOW() - INTERVAL '45 minutes'
FROM agents a WHERE a.name = 'John Smith'
ON CONFLICT DO NOTHING;

-- High priority tickets (8 hour SLA)
INSERT INTO tickets (title, description, priority, status, assigned_agent_id, sla_due_at, created_at, updated_at)
SELECT
    'Email notifications not working',
    'Customer reports that they are not receiving email notifications for order confirmations and shipping updates.',
    'High',
    'InProgress',
    a.id,
    NOW() + INTERVAL '8 hours',
    NOW() - INTERVAL '2 hours',
    NOW() - INTERVAL '30 minutes'
FROM agents a WHERE a.name = 'Sarah Wilson'
ON CONFLICT DO NOTHING;

INSERT INTO tickets (title, description, priority, status, assigned_agent_id, sla_due_at, created_at, updated_at)
SELECT
    'Mobile app crashes on startup',
    'Several users are experiencing app crashes immediately after opening the mobile application on iOS devices.',
    'High',
    'Open',
    a.id,
    NOW() + INTERVAL '8 hours',
    NOW() - INTERVAL '1 hour 40 minutes',
    NOW() - INTERVAL '1 hour 40 minutes'
FROM agents a WHERE a.name = 'Mike Johnson'
ON CONFLICT DO NOTHING;

-- Medium priority tickets (1 day SLA)
INSERT INTO tickets (title, description, priority, status, assigned_agent_id, sla_due_at, created_at, updated_at)
SELECT
    'Password reset feature not working',
    'Users clicking ''Forgot Password'' are not receiving reset emails. The issue seems to be intermittent.',
    'Medium',
    'InProgress',
    a.id,
    NOW() + INTERVAL '1 day',
    NOW() - INTERVAL '18 hours',
    NOW() - INTERVAL '30 minutes'
FROM agents a WHERE a.name = 'Emily Davis'
ON CONFLICT DO NOTHING;

INSERT INTO tickets (title, description, priority, status, assigned_agent_id, sla_due_at, created_at, updated_at)
SELECT
    'Checkout process taking too long',
    'Customers are reporting that the checkout process is unusually slow, especially during payment processing.',
    'Medium',
    'Open',
    a.id,
    NOW() + INTERVAL '1 day',
    NOW() - INTERVAL '22 hours',
    NOW() - INTERVAL '22 hours'
FROM agents a WHERE a.name = 'Alex Brown'
ON CONFLICT DO NOTHING;

INSERT INTO tickets (title, description, priority, status, assigned_agent_id, sla_due_at, created_at, updated_at)
SELECT
    'Search function returning incorrect results',
    'The search functionality is returning results that don''t match the search query. This is confusing users and affecting their experience.',
    'Medium',
    'Open',
    a.id,
    NOW() + INTERVAL '1 day',
    NOW() - INTERVAL '6 hours',
    NOW() - INTERVAL '6 hours'
FROM agents a WHERE a.name = 'Sarah Wilson'
ON CONFLICT DO NOTHING;

-- Low priority tickets (3 day SLA)
INSERT INTO tickets (title, description, priority, status, assigned_agent_id, sla_due_at, created_at, updated_at)
SELECT
    'Profile picture upload failing',
    'Users are unable to upload profile pictures. The upload seems to start but then fails with no error message.',
    'Low',
    'Resolved',
    a.id,
    NOW() - INTERVAL '1 day',
    NOW() - INTERVAL '2 days',
    NOW() - INTERVAL '18 hours'
FROM agents a WHERE a.name = 'John Smith'
ON CONFLICT DO NOTHING;

INSERT INTO tickets (title, description, priority, status, assigned_agent_id, sla_due_at, created_at, updated_at)
SELECT
    'Dark mode toggle not saving preference',
    'Users report that when they toggle dark mode, the preference is not saved and reverts to light mode on next login.',
    'Low',
    'Open',
    a.id,
    NOW() + INTERVAL '3 days',
    NOW() - INTERVAL '14 hours',
    NOW() - INTERVAL '14 hours'
FROM agents a WHERE a.name = 'Mike Johnson'
ON CONFLICT DO NOTHING;

-- Add additional history entries for tickets that have been updated
-- This will be handled automatically by the trigger, but we can add some manual entries for more realistic history

-- Insert additional history for the email notifications ticket
INSERT INTO ticket_history (ticket_id, action, details, agent_name, created_at)
SELECT
    t.id,
    'updated status',
    'Changed status from Open to In Progress',
    'Sarah Wilson',
    NOW() - INTERVAL '30 minutes'
FROM tickets t
WHERE t.title = 'Email notifications not working';

-- Insert additional history for the password reset ticket
INSERT INTO ticket_history (ticket_id, action, details, agent_name, created_at)
SELECT
    t.id,
    'added comment',
    'Investigating email service provider logs',
    'Emily Davis',
    NOW() - INTERVAL '30 minutes'
FROM tickets t
WHERE t.title = 'Password reset feature not working';

-- Insert history for resolved ticket
INSERT INTO ticket_history (ticket_id, action, details, agent_name, created_at)
SELECT
    t.id,
    'updated status',
    'Changed status from Open to In Progress',
    'John Smith',
    NOW() - INTERVAL '1 day 2 hours'
FROM tickets t
WHERE t.title = 'Profile picture upload failing';

INSERT INTO ticket_history (ticket_id, action, details, agent_name, created_at)
SELECT
    t.id,
    'resolved ticket',
    'Fixed file upload size limit issue',
    'John Smith',
    NOW() - INTERVAL '18 hours'
FROM tickets t
WHERE t.title = 'Profile picture upload failing';
