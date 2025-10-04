UPDATE sessions
SET assigned_agent_id = @agent_id, updated_at = @now
WHERE id = @id

