SELECT id, title, description, priority, status, assigned_agent_id, sla_due_at, created_at, updated_at
FROM tickets
WHERE id = @id

