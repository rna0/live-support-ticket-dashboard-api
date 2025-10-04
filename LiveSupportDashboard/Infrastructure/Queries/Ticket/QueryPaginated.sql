SELECT id, title, description, priority, status, assigned_agent_id, sla_due_at, created_at, updated_at
FROM tickets
{whereClause}
ORDER BY created_at DESC
LIMIT @limit OFFSET @offset

