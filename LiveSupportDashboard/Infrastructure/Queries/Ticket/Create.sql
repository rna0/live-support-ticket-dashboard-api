INSERT INTO tickets (title, description, priority, status, assigned_agent_id, sla_due_at, created_at, updated_at)
VALUES (@title, @description, @priority, @status, @assignedAgentId, @slaDueAt, @now, @now)
RETURNING id

